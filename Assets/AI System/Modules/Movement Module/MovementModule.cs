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
    [SerializeField] private MovementProfile movementProfile;

    [Title("Debug")]
    [SerializeField, ReadOnly] private bool isPathValid;
    [SerializeField, ReadOnly] private NavMeshPathStatus currentPathStatus;

    private NavMeshAgent agent;
    private Vector3 spawnPoint;
    private AnimatorModule animatorModule;

    private NavMeshPath cachedPath;
    private Vector3 lastDestination;
    private bool hasDestination;

    public MovementProfile MovementProfile => movementProfile;
    public Vector3 SpawnPoint => spawnPoint;
    public bool IsPathValid => isPathValid;
    public NavMeshPathStatus CurrentPathStatus => currentPathStatus;

    protected Blackboard blackboard { get; private set; }
    public void SetBlackboard(Blackboard _blackboard)
    {
        blackboard = _blackboard;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPoint = transform.position;
        animatorModule = GetComponent<AnimatorModule>();
        cachedPath = new NavMeshPath();

        // Rotation is now fully managed by RotationModule.
        // These are set again during RotationModule.Initialize() for safety,
        // but we set them here too to prevent any frame-0 agent rotation.
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;
    }

    #region Movement

    public bool MoveTo(Vector3 destination)
    {
        if (!IsServer) return false;
        return MoveToInternal(destination);
    }

    private bool MoveToInternal(Vector3 destination)
    {
        agent.CalculatePath(destination, cachedPath);
        currentPathStatus = cachedPath.status;
        isPathValid = cachedPath.status == NavMeshPathStatus.PathComplete;

        if (isPathValid || cachedPath.status == NavMeshPathStatus.PathPartial)
        {
            agent.SetPath(cachedPath);
            lastDestination = destination;
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
        agent.ResetPath();
        hasDestination = false;
        SetSpeedInternal(0);
    }

    public void SetStopped(bool stopped)
    {
        if (!IsServer) return;
        agent.isStopped = stopped;
        if (stopped)
        {
            agent.ResetPath();
            hasDestination = false;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        if (!IsServer) return;
        SetSpeedInternal(newSpeed);
    }

    private void SetSpeedInternal(float newSpeed)
    {
        agent.speed = newSpeed;

        if (animatorModule)
        {
            animatorModule.SetSpeedParam(newSpeed);
        }
    }

    #endregion

    #region Status Checks

    public bool HasArrived()
    {
        if (!IsServer) return false;

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
        if (!hasDestination) return false;

        return agent.velocity.sqrMagnitude < stuckThreshold * stuckThreshold
               && !HasArrived()
               && !agent.pathPending;
    }

    public float GetCurrentSpeed()
    {
        return agent.velocity.magnitude;
    }

    public float GetRemainingDistance()
    {
        if (!hasDestination) return 0f;
        return agent.remainingDistance;
    }

    public bool HasValidPath()
    {
        return hasDestination && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete;
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

        string status = HasArrived() ? "Arrived" :
                       IsStuck() ? "STUCK" :
                       agent.pathPending ? "Calculating..." :
                       "Moving";

        if (!isPathValid && hasDestination)
            status += " (Invalid Path)";

        Handles.Label(
            transform.position + Vector3.up * (agent.height + 0.5f),
            status
        );
    }
#endif
}