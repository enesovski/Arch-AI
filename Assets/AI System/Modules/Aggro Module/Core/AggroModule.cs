using System;
using System.Collections.Generic;
using Artifika.AI;
using Artifika.AI.Aggro;
using Artifika.AI.Aggro.Reactions;
using Artifika.AI.Attack;
using UnityEngine;
using Sirenix.OdinInspector;

public sealed class AggroModule : UpdatableEntityModule
{
    public event Action<EngagementContext> OnContextChanged;
    public event Action<AggroStateChangeEventArgs> OnStateChanged;
    public event Action<TargetChangeEventArgs> OnTargetChanged;

    [Title("Aggro Module")]
    [SerializeField, Required, AssetsOnly, InlineEditor] private AggroPolicy policy;
    
    private DetectionModule detectionModule;
    private AttackModule attackModule;
    private GameEntity selfEntity;

    private EngagementState state;
    private GameEntity currentTarget;
    private EngagementContext context;

    private float engageHoldUntil;
    private float searchEndTime;
    private Vector3 searchOrigin;
    private bool hasSearchOrigin;

    private HealthComponent targetHealth;
    private bool targetDiedThisFrame;

    private readonly List<GameEntity> threatBuffer = new List<GameEntity>(16);
    private readonly List<ThreatCandidate> candidates = new List<ThreatCandidate>(16);

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
    public EngagementState CurrentEngagementState => state;

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
    public AggroState CurrentAggroState => MapToLegacyState(state);

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
    public GameEntity CurrentTarget => currentTarget;

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
    public Vector3 LastKnownPosition => context.LastKnownPosition;


    protected override void OnInitialize()
    {
        selfEntity = blackboard.gameEntity;
        detectionModule = blackboard.detectionModule;
        attackModule = blackboard.attackModule;

        state = EngagementState.None;
        context = CreateDefaultContext();
    }

    public override void PerformUpdate(float deltaTime)
    {
        if (!policy || !detectionModule)
            return;

        float now = Time.time;

        ValidateCurrentTarget();
        BuildCandidates();

        ThreatCandidate best = SelectBestCandidate();
        ThreatCandidate currentCand = FindCandidate(currentTarget);

        EngagementState nextState = EvaluateTransition(now, best, currentCand);
        GameEntity nextTarget = ResolveTarget(nextState, best);

        if (nextTarget != currentTarget)
            SetTarget(nextTarget);

        if (nextState != state)
            ApplyStateTransition(nextState, now);

        currentCand = FindCandidate(currentTarget);
        best = SelectBestCandidate();

        RebuildContext(nextState, currentCand, best);

        state = nextState;
        targetDiedThisFrame = false;
    }

    public override void DestroyModule()
    {
        UnsubscribeTargetDeath();

        if (policy != null && policy.mode == EngagementMode.Aggressive && currentTarget != null)
            currentTarget.UnregisterAggro(selfEntity);
    }
    
    private EngagementState EvaluateTransition(float now, ThreatCandidate best, ThreatCandidate currentCand)
    {
        if (targetDiedThisFrame)
            return OnTargetDiedTransition(best);

        bool attackCommitted = attackModule && attackModule.IsAttackCommitted;

        switch (state)
        {
            case EngagementState.None:
                return EvaluateFromIdle(best);

            case EngagementState.Alert:
                return EvaluateFromAlert(best);

            case EngagementState.Engage:
                return EvaluateFromCombat(now, best, currentCand, attackCommitted);

            case EngagementState.Search:
                return EvaluateFromSearch(now, best);

            default:
                return EngagementState.None;
        }
    }

    private EngagementState EvaluateFromIdle(ThreatCandidate best)
    {
        if (best.Entity && best.Score >= policy.alertEnterThreshold)
            return EngagementState.Alert;

        return EngagementState.None;
    }

    private EngagementState EvaluateFromAlert(ThreatCandidate best)
    {
        if (!best.Entity || best.Score < policy.alertExitThreshold)
            return EngagementState.None;

        if (CanEngage(best))
            return EngagementState.Engage;

        return EngagementState.Alert;
    }

    private EngagementState EvaluateFromCombat(
        float now,
        ThreatCandidate best,
        ThreatCandidate currentCand,
        bool attackCommitted)
    {
        if (now < engageHoldUntil)
            return EngagementState.Engage;

        if (policy.keepEngageWhileAttackCommitted && attackCommitted)
            return EngagementState.Engage;

        if (!currentTarget)
        {
            if (best.Entity && CanEngage(best))
                return EngagementState.Engage;

            if (hasSearchOrigin || best.Entity)
                return EngagementState.Search;

            return EngagementState.None;
        }

        if (IsTargetHardLost(currentTarget))
        {
            CaptureSearchOrigin(currentTarget);
            return EngagementState.Search;
        }

        return EngagementState.Engage;
    }

    private EngagementState EvaluateFromSearch(float now, ThreatCandidate best)
    {
        if (best.Entity && CanEngage(best))
            return EngagementState.Engage;

        if (now >= searchEndTime)
            return EngagementState.None;

        return EngagementState.Search;
    }

    private EngagementState OnTargetDiedTransition(ThreatCandidate best)
    {
        if (best.Entity != null && CanEngage(best))
            return EngagementState.Engage;

        if (best.Entity != null && best.Score >= policy.alertEnterThreshold)
            return EngagementState.Alert;

        return EngagementState.None;
    }

    private GameEntity ResolveTarget(EngagementState nextState, ThreatCandidate best)
    {
        switch (nextState)
        {
            case EngagementState.None:
                return null;

            case EngagementState.Engage:
                return currentTarget != null ? currentTarget : best.Entity;

            case EngagementState.Alert:
                return best.Entity;

            case EngagementState.Search:
                return currentTarget;

            default:
                return null;
        }
    }
    
    private bool CanEngage(ThreatCandidate candidate)
    {
        if (candidate.Entity == null)
            return false;

        if (candidate.Score < policy.engageThreshold)
            return false;

        if (candidate.DistanceToEntity > policy.engageDistance)
            return false;

        if (policy.requireVisibilityToEngage && !candidate.IsVisible)
            return false;

        return true;
    }

    private bool IsTargetHardLost(GameEntity target)
    {
        if (target == null)
            return true;

        ThreatInfo info = detectionModule.GetThreatInfo(target);
        if (info == null)
            return true;

        return info.TimeSinceLastSeen > policy.lostGraceSeconds;
    }
    
    private void ApplyStateTransition(EngagementState nextState, float now)
    {
        AggroState prevLegacy = MapToLegacyState(state);
        AggroState nextLegacy = MapToLegacyState(nextState);

        if (nextState == EngagementState.Engage)
        {
            engageHoldUntil = now + policy.minEngageHoldSeconds;

            if (policy.mode == EngagementMode.Aggressive && currentTarget != null)
                currentTarget.RegisterAggro(selfEntity);
        }

        if (nextState == EngagementState.Search)
            searchEndTime = now + policy.searchDurationSeconds;

        if (state == EngagementState.Engage && nextState != EngagementState.Engage)
        {
            if (policy.mode == EngagementMode.Aggressive && currentTarget != null)
                currentTarget.UnregisterAggro(selfEntity);
        }

        if (state == EngagementState.Search && nextState != EngagementState.Search)
            hasSearchOrigin = false;

        OnStateChanged?.Invoke(new AggroStateChangeEventArgs(prevLegacy, nextLegacy, currentTarget));
    }
    
    private void ValidateCurrentTarget()
    {
        if (!currentTarget)
            currentTarget = null;
    }

    private void SetTarget(GameEntity newTarget)
    {
        if (!newTarget) newTarget = null;
        if (currentTarget == newTarget) return;

        UnsubscribeTargetDeath();

        GameEntity previous = currentTarget;
        currentTarget = newTarget;

        SubscribeTargetDeath(currentTarget);
        OnTargetChanged?.Invoke(new TargetChangeEventArgs(previous, newTarget));
    }

    private void SubscribeTargetDeath(GameEntity target)
    {
        if (target == null) return;

        if (target.TryGetComponent(out HealthComponent health))
        {
            targetHealth = health;
            targetHealth.OnDeath += HandleTargetDeath;
        }
    }

    private void UnsubscribeTargetDeath()
    {
        if (targetHealth == null) return;

        targetHealth.OnDeath -= HandleTargetDeath;
        targetHealth = null;
    }

    private void HandleTargetDeath()
    {
        if (policy != null && policy.mode == EngagementMode.Aggressive
            && state == EngagementState.Engage && currentTarget != null)
        {
            currentTarget.UnregisterAggro(selfEntity);
        }

        targetDiedThisFrame = true;
        hasSearchOrigin = false;
        engageHoldUntil = 0f;

        SetTarget(null);
    }
    
    private void CaptureSearchOrigin(GameEntity target)
    {
        if (target == null)
        {
            searchOrigin = transform.position;
            hasSearchOrigin = true;
            return;
        }

        ThreatInfo info = detectionModule.GetThreatInfo(target);
        searchOrigin = info != null ? info.LastKnownPosition : target.transform.position;
        hasSearchOrigin = true;
    }
    
    private void BuildCandidates()
    {
        candidates.Clear();
        detectionModule.GetThreatEntities(threatBuffer);

        Vector3 selfPos = transform.position;

        for (int i = 0; i < threatBuffer.Count; i++)
        {
            GameEntity entity = threatBuffer[i];
            if (entity == null) continue;

            float score = detectionModule.GetThreatScore(entity);
            if (score <= 0f) continue;

            ThreatInfo info = detectionModule.GetThreatInfo(entity);
            if (info == null) continue;

            float distance = Vector3.Distance(selfPos, entity.transform.position);
            float timeSinceLastSeen = info.TimeSinceLastSeen;

            bool closeRange = policy.closeRangeAssumeVisibleDistance > 0f
                              && distance <= policy.closeRangeAssumeVisibleDistance;

            bool isVisible = closeRange
                             || (info.DetectionStrength > 0f
                                 && timeSinceLastSeen <= Mathf.Max(0.1f, policy.lostGraceSeconds));

            candidates.Add(new ThreatCandidate(
                entity,
                score,
                info.LastKnownPosition,
                timeSinceLastSeen,
                isVisible,
                distance,
                info.HitStrength > 0f));
        }
    }

    private ThreatCandidate SelectBestCandidate()
    {
        if (candidates.Count == 0)
            return default;

        ThreatCandidate best = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
        {
            if (candidates[i].Score > best.Score)
                best = candidates[i];
        }

        return best;
    }

    private ThreatCandidate FindCandidate(GameEntity entity)
    {
        if (!entity) return default;

        foreach (var threatCandidate in candidates)
        {
            if (threatCandidate.Entity == entity)
                return threatCandidate;
        }

        return default;
    }
    
    private void RebuildContext(EngagementState nextState, ThreatCandidate currentCand, ThreatCandidate best)
    {
        ThreatCandidate focus = currentCand.Entity != null ? currentCand : best;

        Vector3 lastKnown = focus.Entity != null
            ? focus.LastKnownPosition
            : (nextState == EngagementState.Search && hasSearchOrigin ? searchOrigin : context.LastKnownPosition);

        float timeSinceLastSeen = focus.Entity != null ? focus.TimeSinceLastSeen : context.TimeSinceLastSeen;
        bool isVisible = focus.Entity != null && focus.IsVisible;
        float distance = focus.Entity != null ? focus.DistanceToEntity : float.PositiveInfinity;
        float score = focus.Entity != null ? focus.Score : 0f;

        EngagementContext next = new EngagementContext(
            nextState,
            focus.Entity,
            score,
            lastKnown,
            timeSinceLastSeen,
            isVisible,
            distance);

        if (!ContextEquals(context, next))
        {
            context = next;
            OnContextChanged?.Invoke(context);
        }
        else
        {
            context = next;
        }
    }
    
    private static AggroState MapToLegacyState(EngagementState engagementState)
    {
        switch (engagementState)
        {
            case EngagementState.Engage:  return AggroState.Alerted;
            case EngagementState.Alert:
            case EngagementState.Search:  return AggroState.Suspicious;
            default:                      return AggroState.Passive;
        }
    }

    private EngagementContext CreateDefaultContext()
    {
        return new EngagementContext(
            EngagementState.None,
            null,
            0f,
            transform.position,
            float.PositiveInfinity,
            false,
            float.PositiveInfinity);
    }

    private static bool ContextEquals(EngagementContext a, EngagementContext b)
    {
        const float epsilon = 0.0001f;

        if (a.State != b.State) return false;
        if (a.Target != b.Target) return false;
        if (Mathf.Abs(a.TargetScore - b.TargetScore) > epsilon) return false;
        if (Mathf.Abs(a.TimeSinceLastSeen - b.TimeSinceLastSeen) > epsilon) return false;
        if (a.IsTargetVisible != b.IsTargetVisible) return false;
        if ((a.LastKnownPosition - b.LastKnownPosition).sqrMagnitude > epsilon) return false;
        if (Mathf.Abs(a.DistanceToTarget - b.DistanceToTarget) > epsilon) return false;

        return true;
    }
}