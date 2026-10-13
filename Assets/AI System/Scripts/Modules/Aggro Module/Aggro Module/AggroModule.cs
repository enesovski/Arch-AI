using System;
using System.Collections.Generic;
using Artifika.AI.Aggro;
using Artifika.AI.Attack;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public sealed class AggroModule : UpdatableEntityModule, IEntityStateModule
    {
        public event Action<EngagementContext> OnContextChanged;
        public event Action<AggroStateChangeEventArgs> OnStateChanged;
        public event Action<TargetChangeEventArgs> OnTargetChanged;
        public event Action<SuspiciousStateChangeEventArgs> OnSuspiciousStateChanged;

        [Title("Aggro Module")] [SerializeField, Required, AssetsOnly, InlineEditor]
        public AggroConfiguration configuration;

        private DetectionModule detectionModule;
        private AttackModule attackModule;
        private GameEntity selfEntity;

        private EngagementState state;
        private SuspiciousState suspiciousState;
        private GameEntity currentTarget;
        private EngagementContext context;

        private float engageHoldUntil;
        private float alertEndTime;
        private Vector3 alertStartPosition;
        private float searchEndTime;
        private Vector3 searchOrigin;
        private bool hasSearchOrigin;
        private float nextTargetSwitchTime;

        private HealthComponent targetHealth;
        private bool targetDiedThisFrame;

        private readonly List<GameEntity> threatBuffer = new List<GameEntity>(16);
        private readonly List<ThreatCandidate> candidates = new List<ThreatCandidate>(16);

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public EngagementState CurrentEngagementState => state;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public SuspiciousState CurrentSuspiciousState => suspiciousState;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public AggroState CurrentAggroState => MapToAggroState(state);

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public GameEntity CurrentTarget => currentTarget;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public Vector3 LastKnownPosition => context.LastKnownPosition;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public Vector3 SearchOrigin => hasSearchOrigin ? searchOrigin : context.LastKnownPosition;

        protected override void OnInitialize()
        {
            selfEntity = blackboard.gameEntity;
            detectionModule = blackboard.detectionModule;
            attackModule = blackboard.attackModule;

            state = EngagementState.Passive;
            suspiciousState = SuspiciousState.None;
            context = CreateDefaultContext();
        }

        public override void PerformUpdate(float deltaTime)
        {
            if (!configuration || !detectionModule)
                return;

            float now = Time.time;

            ValidateCurrentTarget();
            BuildCandidates();

            ThreatCandidate best = SelectBestCandidate();
            ThreatCandidate currentCand = FindCandidate(currentTarget);
            bool attackCommitted = attackModule && attackModule.IsAttackCommitted;

            EngagementState nextState = EvaluateTransition(now, best, attackCommitted);
            GameEntity nextTarget = ResolveTarget(nextState, best, currentCand, now, attackCommitted);

            if (nextTarget != currentTarget)
                SetTarget(nextTarget, nextState);

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

            if (configuration != null && configuration.mode == AggressivenessMode.Aggressive && currentTarget != null)
                currentTarget.AggroTracker?.UnregisterAggro(selfEntity);
        }

        private EngagementState EvaluateTransition(
            float now,
            ThreatCandidate best,
            bool attackCommitted)
        {
            if (targetDiedThisFrame)
                return OnTargetDiedTransition(best);

            switch (state)
            {
                case EngagementState.Passive: return EvaluateFromPassive(best);

                case EngagementState.Alerted: return EvaluateFromAlerted(now, best);

                case EngagementState.Searching: return EvaluateFromSearching(now, best);
                case EngagementState.Engaged: return EvaluateFromEngaged(now, best, attackCommitted);
                default: return EngagementState.Passive;
            }
        }

        private EngagementState EvaluateFromPassive(ThreatCandidate best)
        {
            if (ShouldForceEngageFromRangedHit(best))
                return EngagementState.Engaged;

            if (best.Entity && best.Score >= configuration.alertEnterThreshold)
                return EngagementState.Alerted;

            return EngagementState.Passive;
        }

        private EngagementState EvaluateFromAlerted(float now, ThreatCandidate best)
        {
            if (!best.Entity || best.Score < configuration.alertExitThreshold)
                return EngagementState.Passive;

            if (ShouldForceEngageFromRangedHit(best) || CanEngage(best))
                return EngagementState.Engaged;

            if (ShouldSearchFromAlerted(now, best))
            {
                searchOrigin = best.LastKnownPosition;
                hasSearchOrigin = true;
                return EngagementState.Searching;
            }

            return EngagementState.Alerted;
        }

        private EngagementState EvaluateFromSearching(float now, ThreatCandidate best)
        {
            if (ShouldForceEngageFromRangedHit(best))
                return EngagementState.Engaged;

            if (best.Entity && CanEngage(best))
                return EngagementState.Engaged;

            if (now >= searchEndTime)
                return EngagementState.Passive;

            return EngagementState.Searching;
        }

        private EngagementState EvaluateFromEngaged(
            float now,
            ThreatCandidate best,
            bool attackCommitted)
        {
            if (now < engageHoldUntil)
                return EngagementState.Engaged;

            if (configuration.keepEngageWhileAttackCommitted && attackCommitted)
                return EngagementState.Engaged;

            if (!currentTarget)
            {
                if (best.Entity && CanEngage(best))
                    return EngagementState.Engaged;

                if (!hasSearchOrigin && best.Entity)
                {
                    searchOrigin = best.LastKnownPosition;
                    hasSearchOrigin = true;
                }

                if (hasSearchOrigin)
                    return EngagementState.Searching;

                return EngagementState.Passive;
            }

            if (IsTargetHardLost(currentTarget))
            {
                CaptureSearchOrigin(currentTarget);
                return EngagementState.Searching;
            }

            return EngagementState.Engaged;
        }

        private EngagementState OnTargetDiedTransition(ThreatCandidate best)
        {
            if (best.Entity != null && CanEngage(best))
                return EngagementState.Engaged;

            if (best.Entity != null && best.Score >= configuration.alertEnterThreshold)
                return EngagementState.Alerted;

            return EngagementState.Passive;
        }

        private GameEntity ResolveTarget(
            EngagementState nextState,
            ThreatCandidate best,
            ThreatCandidate currentCand,
            float now,
            bool attackCommitted)
        {
            switch (nextState)
            {
                case EngagementState.Passive: return null;
                case EngagementState.Alerted: return best.Entity;
                case EngagementState.Searching: return currentTarget;
                case EngagementState.Engaged:
                    if (currentTarget == null)
                        return best.Entity;

                    return ShouldSwitchTarget(best, currentCand, now, attackCommitted)
                        ? best.Entity
                        : currentTarget;
                default: return null;
            }
        }

        private bool CanEngage(ThreatCandidate candidate)
        {
            if (candidate.Entity == null)
                return false;

            if (candidate.Score < configuration.engageThreshold)
                return false;

            if (candidate.DistanceToEntity > configuration.engageDistance)
                return false;

            if (configuration.requireVisibilityToEngage && !candidate.IsVisible)
                return false;

            return true;
        }

        private bool ShouldSwitchTarget(
            ThreatCandidate best,
            ThreatCandidate currentCand,
            float now,
            bool attackCommitted)
        {
            if (state != EngagementState.Engaged)
                return false;

            if (best.Entity == null || best.Entity == currentTarget)
                return false;

            if (now < engageHoldUntil || now < nextTargetSwitchTime)
                return false;

            if (configuration.keepEngageWhileAttackCommitted && attackCommitted)
                return false;

            if (!CanEngage(best))
                return false;

            float currentScore = currentCand.Entity != null ? currentCand.Score : 0f;
            return best.Score >= currentScore + configuration.targetSwitchScoreMargin;
        }

        private bool ShouldForceEngageFromRangedHit(ThreatCandidate candidate)
        {
            if (candidate.Entity == null || !candidate.HasRangedHitPressure)
                return false;

            EntityConfiguration entityConfig = selfEntity != null ? selfEntity.entityConfiguration : null;
            bool passiveEntity = selfEntity != null && selfEntity.EntityType == EntityType.Passive;

            return passiveEntity && entityConfig != null && entityConfig.rangedHitForcesEngage;
        }

        private bool IsTargetHardLost(GameEntity target)
        {
            if (target == null)
                return true;

            ThreatInfo info = detectionModule.GetThreatInfo(target);
            if (info == null)
                return true;

            return info.TimeSinceLastSeen > configuration.lostGraceSeconds;
        }

        private void ApplyStateTransition(EngagementState newState, float now)
        {
            AggroState prevAggroState = MapToAggroState(state);
            AggroState nextAggroState = MapToAggroState(newState);

            if (newState == EngagementState.Engaged)
            {
                engageHoldUntil = now + configuration.minEngageHoldSeconds;

                if (configuration.mode == AggressivenessMode.Aggressive && currentTarget != null)
                    currentTarget.AggroTracker?.RegisterAggro(selfEntity);
            }

            if (newState == EngagementState.Alerted && state != EngagementState.Alerted)
            {
                alertEndTime = now + configuration.suspiciousApproachSeconds;
                alertStartPosition = transform.position;
                hasSearchOrigin = false;
            }

            if (newState == EngagementState.Searching)
            {
                searchEndTime = now + configuration.searchDurationSeconds;

                if (state != EngagementState.Searching)
                    EnsureSearchOrigin();
            }

            if (state == EngagementState.Engaged && newState != EngagementState.Engaged)
            {
                if (configuration.mode == AggressivenessMode.Aggressive && currentTarget != null)
                    currentTarget.AggroTracker?.UnregisterAggro(selfEntity);
            }

            if (state == EngagementState.Searching && newState != EngagementState.Searching)
                hasSearchOrigin = false;

            if (prevAggroState != nextAggroState)
                OnStateChanged?.Invoke(new AggroStateChangeEventArgs(prevAggroState, nextAggroState));

            SuspiciousState prevSuspicious = MapToSuspiciousState(state);
            SuspiciousState nextSuspicious = MapToSuspiciousState(newState);

            if (prevSuspicious != nextSuspicious)
            {
                suspiciousState = nextSuspicious;
                OnSuspiciousStateChanged?.Invoke(
                    new SuspiciousStateChangeEventArgs(prevSuspicious, nextSuspicious));
            }
        }

        private void ValidateCurrentTarget()
        {
            if (!currentTarget)
                currentTarget = null;
        }

        private void SetTarget(GameEntity newTarget)
        {
            SetTarget(newTarget, state);
        }

        private void SetTarget(GameEntity newTarget, EngagementState nextState)
        {
            if (!newTarget) newTarget = null;
            if (currentTarget == newTarget) return;

            UnsubscribeTargetDeath();

            GameEntity previous = currentTarget;
            currentTarget = newTarget;

            TransferAggroRegistration(previous, currentTarget, nextState);

            SubscribeTargetDeath(currentTarget);
            OnTargetChanged?.Invoke(new TargetChangeEventArgs(previous, newTarget));
        }

        private void TransferAggroRegistration(
            GameEntity previousTarget,
            GameEntity nextTarget,
            EngagementState nextState)
        {
            if (configuration == null || configuration.mode != AggressivenessMode.Aggressive)
                return;

            bool targetChanged = previousTarget != nextTarget;
            if (!targetChanged)
                return;

            if (state == EngagementState.Engaged && previousTarget != null)
                previousTarget.AggroTracker?.UnregisterAggro(selfEntity);

            if (nextState == EngagementState.Engaged && nextTarget != null)
            {
                nextTarget.AggroTracker?.RegisterAggro(selfEntity);
                nextTargetSwitchTime = Time.time + configuration.targetSwitchCooldownSeconds;
            }
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
            if (configuration != null && configuration.mode == AggressivenessMode.Aggressive
                               && state == EngagementState.Engaged && currentTarget != null)
            {
                currentTarget.AggroTracker?.UnregisterAggro(selfEntity);
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
                searchOrigin = context.LastKnownPosition;
                hasSearchOrigin = true;
                return;
            }

            ThreatInfo info = detectionModule.GetThreatInfo(target);
            searchOrigin = info != null ? info.LastKnownPosition : target.transform.position;
            hasSearchOrigin = true;
        }

        private void EnsureSearchOrigin()
        {
            if (hasSearchOrigin)
                return;

            CaptureSearchOrigin(currentTarget);
        }

        private bool ShouldSearchFromAlerted(float now, ThreatCandidate best)
        {
            if (configuration.mode != AggressivenessMode.Aggressive)
                return false;

            if (now >= alertEndTime)
                return true;

            Vector3 currentPosition = transform.position;

            if (Vector3.Distance(currentPosition, best.LastKnownPosition) <= configuration.suspiciousApproachStopDistance)
                return true;

            float travelled = Vector3.Distance(alertStartPosition, currentPosition);
            return travelled >= configuration.suspiciousApproachMaxDistance;
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

                bool closeRange = configuration.closeRangeAssumeVisibleDistance > 0f
                                  && distance <= configuration.closeRangeAssumeVisibleDistance;

                bool isVisible = closeRange
                                 || (info.DetectionStrength > 0f
                                     && timeSinceLastSeen <= Mathf.Max(0.1f, configuration.lostGraceSeconds));

                candidates.Add(new ThreatCandidate(
                    entity,
                    score,
                    info.LastKnownPosition,
                    timeSinceLastSeen,
                    isVisible,
                    distance,
                    info.LastHitDeliveryType == DamageDeliveryType.Ranged ||
                    info.LastHitDeliveryType == DamageDeliveryType.Projectile));
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

            foreach (var candidate in candidates)
            {
                if (candidate.Entity == entity)
                    return candidate;
            }

            return default;
        }

        private void RebuildContext(EngagementState nextState, ThreatCandidate currentCand, ThreatCandidate best)
        {
            ThreatCandidate focus = currentCand.Entity != null ? currentCand : best;

            Vector3 lastKnown = nextState == EngagementState.Searching && hasSearchOrigin
                ? searchOrigin
                : (focus.Entity != null
                    ? focus.LastKnownPosition
                    : context.LastKnownPosition);

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

            bool changed = !ContextEquals(context, next);
            context = next;
            if (changed)
                OnContextChanged?.Invoke(context);
        }

        private static AggroState MapToAggroState(EngagementState e) => e switch
        {
            EngagementState.Engaged => AggroState.Alerted,
            EngagementState.Alerted or
                EngagementState.Searching => AggroState.Suspicious,
            _ => AggroState.Passive
        };

        private static SuspiciousState MapToSuspiciousState(EngagementState e) => e switch
        {
            EngagementState.Alerted => SuspiciousState.Alerted,
            EngagementState.Searching => SuspiciousState.Searching,
            _ => SuspiciousState.None
        };

        private EngagementContext CreateDefaultContext()
        {
            return new EngagementContext(
                EngagementState.Passive,
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

        private readonly struct ThreatCandidate
        {
            public GameEntity Entity { get; }
            public float Score { get; }
            public Vector3 LastKnownPosition { get; }
            public float TimeSinceLastSeen { get; }
            public bool IsVisible { get; }
            public float DistanceToEntity { get; }
            public bool HasRangedHitPressure { get; }

            public ThreatCandidate(
                GameEntity entity,
                float score,
                Vector3 lastKnownPosition,
                float timeSinceLastSeen,
                bool isVisible,
                float distanceToEntity,
                bool hasRangedHitPressure)
            {
                Entity = entity;
                Score = score;
                LastKnownPosition = lastKnownPosition;
                TimeSinceLastSeen = timeSinceLastSeen;
                IsVisible = isVisible;
                DistanceToEntity = distanceToEntity;
                HasRangedHitPressure = hasRangedHitPressure;
            }
        }
    }
}
