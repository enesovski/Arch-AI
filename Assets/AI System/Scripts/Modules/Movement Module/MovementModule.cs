using System.Linq;
using Artifika.AI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode.Components;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(NavMeshAgent), typeof(NetworkObject), typeof(NetworkTransform))]
public class MovementModule : NetworkBehaviour
{
    [Title("Movement")]
    [Tooltip("Yürüme, yaklaşma/Rush ve konum değiştirme hızları ile adım mesafeleri bu profildedir. Konum/mesafe tercihleri Melee veya Ranged Combat Profile içindedir.")]
    [SerializeField] private MovementProfile movementProfile;

    [Title("Melee Combat")]
    [Tooltip("ChaseTarget için isteğe bağlı yakın dövüş konumlanması. Ön/yan bölge, geri çekilme mesafesi ve tekrar yaklaşmadan önceki toplam süre buradadır. Hızlar ve adımlar arası bekleme Movement Profile içindedir.")]
    [SerializeField, InlineEditor] private MeleeCombatProfile meleeCombatProfile;

    [Title("Ranged Combat")]
    [Tooltip("İsteğe bağlı menzilli konumlanma: tercih edilen atış mesafesi, yakın oyuncudan kısa kaçış ve atışlar arasında açık konuma geçme. Hızlar/adım mesafeleri Movement Profile içindedir. Melee Combat Profile ile aynı anda kullanılmaz.")]
    [SerializeField, InlineEditor] private RangedCombatProfile rangedCombatProfile;

    private RangedCombatMovement rangedCombatMovement;
    public RangedCombatProfile RangedCombatProfile => rangedCombatProfile;
    public bool UsesRangedCombatMovement => rangedCombatProfile && movementProfile && blackboard &&
        blackboard.attackModule && blackboard.attackModule.HasRangedAttackModule && !UsesMeleeCombatMovement;
    [ShowInInspector, ReadOnly, LabelText("Ranged Phase")]
    public RangedMovementPhase RangedPhase => rangedCombatMovement?.Phase ?? RangedMovementPhase.Aim;

    private MeleeCombatMovement meleeCombatMovement;
    public MeleeCombatProfile MeleeCombatProfile => meleeCombatProfile;
    public bool UsesMeleeCombatMovement => meleeCombatProfile && blackboard &&
        blackboard.attackModule && blackboard.attackModule.HasMeleeAttackModule;
    public bool SuppressRootMotionForAttack => (UsesMeleeCombatMovement || UsesRangedCombatMovement) &&
        blackboard.attackModule.IsAttackCommitted && blackboard.attackModule.CurrentAttackStopsMovement;

    [ShowInInspector, ReadOnly, LabelText("Melee Phase")]
    public MeleeMovementPhase MeleePhase => meleeCombatMovement?.Phase ?? MeleeMovementPhase.Approach;

    [ShowInInspector, ReadOnly, LabelText("Circling Paused")]
    public bool IsMeleeCirclingPaused => meleeCombatMovement?.IsCirclingPaused ?? false;

    [Title("Debug")]
    [SerializeField, ReadOnly] private bool isPathValid;
    [SerializeField, ReadOnly] private NavMeshPathStatus currentPathStatus;

    private NavMeshAgent agent;
    private Vector3 spawnPoint;
    private AnimatorModule animatorModule;
    private HealthComponent healthComponent;

    private NavMeshPath cachedPath;
    private bool hasDestination;
    private bool movementLockedByDeath;

    public MovementProfile MovementProfile => movementProfile;
    public Vector3 SpawnPoint => spawnPoint;
    public bool AutoBraking => agent != null && agent.autoBraking;
    public float StoppingDistance => agent != null ? agent.stoppingDistance : 0f;
    public float AgentRadius => agent != null ? agent.radius : 0f;
    internal bool CanNavigate => !movementLockedByDeath && CanUseAgent();

    protected Blackboard blackboard { get; private set; }
    public void SetBlackboard(Blackboard _blackboard)
    {
        ResetMeleeCombat();
        ResetRangedCombat();
        blackboard = _blackboard;
        meleeCombatMovement = null;
        rangedCombatMovement = null;
        agent = blackboard.agent;
        animatorModule = blackboard.animatorModule;
        cachedPath = new NavMeshPath();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        SubscribeToDeath(blackboard.healthComponent);

        movementLockedByDeath = healthComponent != null && !healthComponent.IsAlive;
        if (movementLockedByDeath)
            StopAndLockForDeath();
    }
    
    public void SetSpawnPoint(Vector3 point)
    {
        if (!IsServer) return;
        spawnPoint = point;
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;
        if (IsServer && spawnPoint == Vector3.zero)
        {
            spawnPoint = transform.position;
        }

        if (IsServer && healthComponent != null && !healthComponent.IsAlive)
            StopAndLockForDeath();
    }

    public override void OnNetworkDespawn()
    {
        ResetMeleeCombat();
        ResetRangedCombat();
        UnsubscribeFromDeath();
        base.OnNetworkDespawn();
    }

    private void OnDisable()
    {
        ResetMeleeCombat();
        ResetRangedCombat();
    }

    private void OnDestroy()
    {
        ResetMeleeCombat();
        ResetRangedCombat();
        UnsubscribeFromDeath();
    }
    
    public void BeginMeleeMovement()
    {
        if (UsesMeleeCombatMovement) GetMeleeCombatMovement().BeginMeleeMovement();
    }

    public bool UpdateMeleeMovement(GameEntity target)
    {
        if (!UsesMeleeCombatMovement)
        {
            ResetMeleeCombat();
            return false;
        }
        return GetMeleeCombatMovement().UpdateMeleeMovement(target);
    }

    // One controller per opted-in entity; no allocation on movement ticks.
    private MeleeCombatMovement GetMeleeCombatMovement() =>
        meleeCombatMovement ??= new MeleeCombatMovement(this, blackboard);

    public void EndMeleeMovement() => meleeCombatMovement?.EndMeleeMovement();
    public void ResetMeleeCombat() => meleeCombatMovement?.ResetMeleeCombat();

    internal void BeginMeleeWithdrawal(GameEntity target)
    {
        if (UsesMeleeCombatMovement) GetMeleeCombatMovement().BeginMeleeWithdrawal(target);
    }

    internal bool IsMeleePositionReady(GameEntity target)
    {
        return !UsesMeleeCombatMovement || GetMeleeCombatMovement().IsPositionReady(target);
    }

    private RangedCombatMovement GetRangedCombatMovement() =>
        rangedCombatMovement ??= new RangedCombatMovement(this, blackboard);

    public bool UpdateRangedMovement(GameEntity target)
    {
        if (!UsesRangedCombatMovement) { ResetRangedCombat(); return false; }
        return GetRangedCombatMovement().Update(target);
    }

    public void EndRangedMovement() => rangedCombatMovement?.End();
    public void ResetRangedCombat() => rangedCombatMovement?.Reset();

    internal bool IsRangedPositionReady(GameEntity target) =>
        !UsesRangedCombatMovement || GetRangedCombatMovement().IsPositionReady(target);

    internal void BeginRangedReposition(GameEntity target)
    {
        if (UsesRangedCombatMovement) GetRangedCombatMovement().BeginRecovery(target);
    }

    internal void BeginRangedAttack(GameEntity target)
    {
        if (UsesRangedCombatMovement) GetRangedCombatMovement().BeginAttack(target);
    }

    public bool Teleport(Vector3 position)
    {
        if (!IsServer) return false;
        if (movementLockedByDeath) return false;

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position); 
            hasDestination = false;
            return true;
        }

        return false;
    }

    #region Movement

    public bool MoveTo(Vector3 destination)
    {
        if (!IsServer) return false;
        if (movementLockedByDeath) return false;
        return MoveToInternal(destination);
    }

    public bool MoveToCompletePath(Vector3 destination)
    {
        if (!IsServer || movementLockedByDeath) return false;
        return MoveToInternal(destination, false);
    }

    public bool TrySampleNavigationPosition(Vector3 position, float maxDistance, out Vector3 sampled)
    {
        sampled = position;
        if (!CanUseAgent()) return false;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, filter)) return false;
        sampled = hit.position;
        return true;
    }

    internal bool HasStraightNavigationTo(Vector3 position)
    {
        return CanUseAgent() && !agent.Raycast(position, out NavMeshHit _);
    }

    internal bool TrySampleMeleeNavigationPosition(Vector3 position, float maxDistance, out Vector3 sampled)
    {
        if (TrySampleNavigationPosition(position, maxDistance, out sampled)) return true;
        if (!CanUseAgent()) return false;
        float plannedHeight = position.y;
        float navigationHeight = agent.nextPosition.y;
        // Holding/orbit points inherit the target's root height. On slopes or
        // entities whose root is above their ground, that height can be farther
        // than the sample radius from the local floor. Try our actual NavMesh
        // height without widening horizontal sampling or relaxing path checks.
        if (Mathf.Abs(plannedHeight - navigationHeight) >= 0.05f)
        {
            position.y = navigationHeight;
            if (TrySampleNavigationPosition(position, maxDistance, out sampled)) return true;
        }

        // The floor at an intermediate point on a slope can differ from both
        // endpoint heights. Resolve that point's actual ground before sampling.
        // This bounded fallback runs only when the two cheap NavMesh queries fail.
        float padding = Mathf.Max(2f, agent.height);
        float upper = Mathf.Max(plannedHeight, navigationHeight);
        float lower = Mathf.Min(plannedHeight, navigationHeight);
        position.y = upper + padding;
        int groundMask = blackboard && blackboard.fitter
            ? blackboard.fitter.GroundLayerMask.value : Physics.DefaultRaycastLayers;
        if (!Physics.Raycast(position, Vector3.down, out RaycastHit ground,
                upper - lower + padding * 2f, groundMask, QueryTriggerInteraction.Ignore)) return false;
        return TrySampleNavigationPosition(ground.point, maxDistance, out sampled);
    }

    public void SetStoppingDistance(float distance)
    {
        if (!IsServer || !CanUseAgent()) return;
        agent.stoppingDistance = Mathf.Max(0f, distance);
    }

    private bool MoveToInternal(Vector3 destination, bool allowPartial = true)
    {
        if (!CanUseAgent())
            return false;

        agent.CalculatePath(destination, cachedPath);
        // Tactical candidates must be reachable. A rejected candidate must not
        // erase the last usable destination or replace it with a partial path.
        if (!allowPartial && cachedPath.status != NavMeshPathStatus.PathComplete)
            return false;
        currentPathStatus = cachedPath.status;
        isPathValid = cachedPath.status == NavMeshPathStatus.PathComplete;

        if (isPathValid || cachedPath.status == NavMeshPathStatus.PathPartial)
        {
            agent.SetPath(cachedPath);
            hasDestination = true;
            return true;
        }

        hasDestination = false;
        return false;
    }

    public void Stop()
    {
        if (!IsServer) return;
        StopInternal();
    }

    private void StopInternal()
    {
        if (!CanUseAgent())
        {
            hasDestination = false;
            isPathValid = false;
            currentPathStatus = NavMeshPathStatus.PathInvalid;
            return;
        }

        agent.ResetPath();
        hasDestination = false;
        isPathValid = false;
        currentPathStatus = NavMeshPathStatus.PathInvalid;
        SetSpeedInternal(0);
    }

    public void SetStopped(bool stopped)
    {
        if (!IsServer) return;
        if (movementLockedByDeath)
        {
            StopAndLockForDeath();
            return;
        }

        if (!CanUseAgent()) return;

        if (stopped)
        {
            agent.ResetPath();
            hasDestination = false;
            isPathValid = false;
            currentPathStatus = NavMeshPathStatus.PathInvalid;
            if (animatorModule)
            {
                animatorModule.SetSpeedParam(0f);
            }
        }
        // ResetPath can clear the native stopped flag. Apply the requested
        // state after clearing the path so an intentional pause stays stopped.
        agent.isStopped = stopped;
    }

    public void SetAutoBraking(bool autoBraking)
    {
        if (!IsServer) return;
        if (!CanUseAgent()) return;

        agent.autoBraking = autoBraking;
    }

    public void SetSpeed(float newSpeed)
    {
        if (!IsServer) return;
        if (movementLockedByDeath)
        {
            SetSpeedInternal(0f);
            return;
        }

        SetSpeedInternal(newSpeed);
    }

    private void SetSpeedInternal(float newSpeed)
    {
        if (!CanUseAgent())
            return;

        agent.speed = newSpeed;

        if (animatorModule)
        {
            animatorModule.SetSpeedParam(newSpeed);
        }
    }

    #endregion

    #region  Getters

    public float CalculateMinSpeed()
    {
        float[] candidates = new[]
        {
            movementProfile.walkMovementSpeed, movementProfile.searchMovementSpeed, 
            movementProfile.runMovementSpeed
        }.Where(s => s > 0f).ToArray();
         
        float minSpeed = candidates.Min();
        return Mathf.Max(0.01f, minSpeed);
    }

    public float CalculateMaxSpeed()
    {
        float[] candidates = new[]
        {
            movementProfile.walkMovementSpeed, movementProfile.searchMovementSpeed, 
            movementProfile.runMovementSpeed
        }.Where(s => s > 0f).ToArray();

        if (candidates.Length == 0) return 2f;

        return candidates.Max();
    }

    #endregion
    
    #region Status

    public bool HasBlockedPath => hasDestination && !isPathValid;

    public bool HasArrived()
    {
        if (!IsServer) return false;
        if (movementLockedByDeath) return true;
        if (!CanUseAgent()) return true;

        if (!hasDestination)
            return true;

        if (agent.pathPending)
            return false;

        bool hasArrived = agent.remainingDistance <= agent.stoppingDistance;

        if (hasArrived && animatorModule != null)
        {
            animatorModule.SetSpeedParam(0f);
        }

        return hasArrived;
    }

    public bool IsStuck(float stuckThreshold = 0.05f)
    {
        if (!IsServer) return false;
        if (movementLockedByDeath) return false;
        if (!CanUseAgent()) return false;
        if (!hasDestination) return false;

        return agent.velocity.sqrMagnitude < stuckThreshold * stuckThreshold
               && !HasArrived()
               && !agent.pathPending;
    }

    public bool HasContinuousPathTo(Vector3 destination)
    {
        if (!IsServer) return false;
        if (movementLockedByDeath) return false;
        if (!CanUseAgent()) return false;
        
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(destination, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }
    
    public bool IsPositionOnNavMesh(Vector3 position, float maxDistance = 2.0f)
    {
        return NavMesh.SamplePosition(position, out _, maxDistance, NavMesh.AllAreas);
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void SubscribeToDeath(HealthComponent newHealthComponent)
    {
        if (healthComponent == newHealthComponent)
            return;

        UnsubscribeFromDeath();
        healthComponent = newHealthComponent;

        if (healthComponent != null)
            healthComponent.OnServerDeathArgs += HandleServerDeath;
    }

    private void UnsubscribeFromDeath()
    {
        if (healthComponent == null)
            return;

        healthComponent.OnServerDeathArgs -= HandleServerDeath;
        healthComponent = null;
    }

    private void HandleServerDeath(DamageInstance killingBlow)
    {
        if (!IsServer) return;
        StopAndLockForDeath();
    }

    private void StopAndLockForDeath()
    {
        ResetMeleeCombat();
        ResetRangedCombat();
        movementLockedByDeath = true;
        hasDestination = false;

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            agent.speed = 0f;
        }

        if (animatorModule)
            animatorModule.SetSpeedParam(0f);
    }
    #endregion

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (agent == null) return;

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, agent.stoppingDistance);

        if (agent.hasPath)
        {
            Gizmos.color = isPathValid ? Color.cyan : Color.yellow;
            Vector3 prev = transform.position;
            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawLine(prev, corner);
                prev = corner;
            }
        }

        if (hasDestination)
        {
            Gizmos.color = isPathValid ? Color.green : Color.red;
            Gizmos.DrawSphere(agent.destination, 0.25f);
        }

        string status = IsMeleeCirclingPaused ? "Circle Pause" :
                       !hasDestination ? "No Path" :
                       HasArrived() ? "Arrived" :
                       IsStuck() ? "STUCK" :
                       agent.pathPending ? "Calculating..." :
                       "Moving";

        if (!isPathValid && hasDestination)
            status += " (Invalid Path)";
        if (UsesMeleeCombatMovement)
            status += $"\nMelee: {MeleePhase}";
        if (UsesRangedCombatMovement)
            status += $"\nRanged: {RangedPhase}";

        Handles.Label(
            transform.position + Vector3.up * (agent.height + 0.5f),
            status
        );
    }
#endif
}
