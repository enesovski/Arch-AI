using Artifika.AI.Attack;
using UnityEngine;

namespace Artifika.AI
{
    public enum MeleeMovementPhase { Approach, Withdraw, Circle, Rush }

    // Owned by MovementModule and ticked by the chase graph.
    internal sealed partial class MeleeCombatMovement
    {
        private readonly MovementModule movementModule;
        private readonly Blackboard blackboard;
        private Transform transform => movementModule.transform;
        private AttackModule attackModule => blackboard.attackModule;
        private RotationModule rotationModule => blackboard.rotationModule;
        private HealthComponent healthComponent => blackboard.healthComponent;
        private MeleeCombatProfile meleeCombatProfile => movementModule.MeleeCombatProfile;
        private MovementProfile navigation => movementModule.MovementProfile;
        private bool UsesMeleeCombatMovement => movementModule.UsesMeleeCombatMovement;
        private bool IsAttackCommitted => attackModule && attackModule.IsAttackCommitted;
        private int MeleeLayerMask => attackModule.MeleeLayerMask;
        private MeleeMovementPhase meleePhase;
        public MeleeMovementPhase Phase => meleePhase;
        public bool IsCirclingPaused => circlePaused;
        private bool UsesCirclingSteps => navigation && navigation.combatStepCircling;
        private bool HoldsGround => meleeCombatProfile.recoveryMode == MeleeRecoveryMode.HoldGround;

        public MeleeCombatMovement(MovementModule movementModule, Blackboard blackboard)
        {
            this.movementModule = movementModule;
            this.blackboard = blackboard;
        }

        private GameEntity movementTarget;
        private bool meleeMovementActive;
        private float phaseStartedAt;
        private float circleUntil;
        private float nextMeleePlanAt;
        private float lastMeleePlanAt;
        private float sectorYaw;
        private float circleDirection;
        private float flankSide = 1f;
        private float rushRangeAdjustment;
        private bool rushEndpointReached;
        private bool circlePaused;
        private float circlePauseUntil;
        private float circleStepProgressAt;
        private float bestCircleStepDistance;
        private float blockedSince = -1f;
        private float sectorRecoverySince = -1f;
        private float bestSectorError = float.PositiveInfinity;
        private bool allowFallbackAttack;
        private Vector3 meleeDestination;
        private bool hasMeleeDestination;
        private float savedStoppingDistance;
        private bool savedAutoBraking;
        private bool savedRootMotionSuppression;
        private RotationMode savedMovementRotation;

        public bool IsPositionReady(GameEntity target)
        {
            if (!UsesMeleeCombatMovement) return true;
            if (!target || (movementTarget == target &&
                meleePhase != MeleeMovementPhase.Approach && meleePhase != MeleeMovementPhase.Rush)) return false;

            Vector3 radial = FlatDirection(transform.position - target.transform.position);
            Vector3 forward = FlatDirection(target.CombatForward);
            float angle = Vector3.SignedAngle(forward, radial, Vector3.up);
            if (!allowFallbackAttack && !IsInPreferredSector(angle, 5f)) return false;

            Vector3 toTarget = FlatDirection(target.AimPosition - transform.position);
            return Vector3.Dot(FlatDirection(transform.forward), toTarget) >=
                   Mathf.Cos(meleeCombatProfile.attackFacingHalfAngle * Mathf.Deg2Rad);
        }

        public void BeginMeleeMovement()
        {
            if (!UsesMeleeCombatMovement || meleeMovementActive || !movementModule || !movementModule.IsServer)
                return;

            savedStoppingDistance = movementModule.StoppingDistance;
            savedAutoBraking = movementModule.AutoBraking;
            movementModule.SetStoppingDistance(navigation ? navigation.combatWaypointTolerance : 0.3f);
            movementModule.SetAutoBraking(true);
            movementModule.SetStopped(false);
            if (rotationModule) savedMovementRotation = rotationModule.CurrentMode;
            if (blackboard.fitter)
            {
                savedRootMotionSuppression = blackboard.fitter.SuppressRootMotionTranslation;
                blackboard.fitter.SuppressRootMotionTranslation = true;
            }
            meleeMovementActive = true;
            hasMeleeDestination = false;
            nextMeleePlanAt = 0f;
        }

        public void EndMeleeMovement()
        {
            if (!meleeMovementActive) return;
            movementModule?.SetStoppingDistance(savedStoppingDistance);
            movementModule?.SetAutoBraking(savedAutoBraking);
            if (blackboard && blackboard.fitter)
                blackboard.fitter.SuppressRootMotionTranslation = savedRootMotionSuppression;
            if (rotationModule && !IsAttackCommitted)
            {
                rotationModule.ClearLookTarget();
                rotationModule.SetMode(savedMovementRotation);
            }
            meleeMovementActive = false;
            hasMeleeDestination = false;
            circlePaused = false;
        }

        public void ResetMeleeCombat()
        {
            EndMeleeMovement();
            movementTarget = null;
            meleePhase = MeleeMovementPhase.Approach;
            blockedSince = -1f;
            ResetSectorRecovery();
            allowFallbackAttack = false;
            rushRangeAdjustment = 0f;
            rushEndpointReached = false;
        }

        private void SetMeleePhase(MeleeMovementPhase phase)
        {
            meleePhase = phase;
            phaseStartedAt = Time.time;
            nextMeleePlanAt = 0f;
            hasMeleeDestination = false;
            rushRangeAdjustment = 0f;
            rushEndpointReached = false;
            circlePaused = false;
            if (phase == MeleeMovementPhase.Circle)
                circleUntil = Time.time + meleeCombatProfile.circleDuration +
                              Random.Range(0f, meleeCombatProfile.circleDurationVariance);
        }

        private void InitializeMeleeTarget(GameEntity newTarget)
        {
            movementTarget = newTarget;
            Vector3 forward = FlatDirection(newTarget.CombatForward);
            sectorYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            circleDirection = Random.value < 0.5f ? -1f : 1f;
            float initialAngle = Vector3.SignedAngle(forward,
                FlatDirection(transform.position - newTarget.transform.position), Vector3.up);
            flankSide = Mathf.Abs(initialAngle) > 1f ? Mathf.Sign(initialAngle) : circleDirection;
            lastMeleePlanAt = Time.time;
            blockedSince = -1f;
            ResetSectorRecovery();
            allowFallbackAttack = false;
            SetMeleePhase(MeleeMovementPhase.Approach);
        }

        public void BeginMeleeWithdrawal(GameEntity target)
        {
            if (!target) return;
            if (movementTarget != target) InitializeMeleeTarget(target);
            SetMeleePhase(MeleeMovementPhase.Withdraw);
            if (HoldsGround)
            {
                withdrawalStart = transform.position;
                withdrawalDirection = FlatDirection(transform.position - target.transform.position);
                withdrawalDistance = Mathf.Max(0f, navigation ? navigation.combatWithdrawalStepDistance : 2f);
                withdrawalFinished = false;
            }
        }

        public bool UpdateMeleeMovement(GameEntity chaseTarget)
        {
            if (!UsesMeleeCombatMovement) return false;
            if (!movementModule || !movementModule.IsServer || !chaseTarget || IsAttackCommitted ||
                (healthComponent && !healthComponent.IsAlive)) return true;

            BeginMeleeMovement();
            if (movementTarget != chaseTarget) InitializeMeleeTarget(chaseTarget);
            // Rush must stop at its endpoint between planning ticks.
            if (meleePhase == MeleeMovementPhase.Rush && hasMeleeDestination && movementModule.HasArrived())
            {
                movementModule.SetStopped(true);
                movementModule.SetSpeed(0f);
                // SetStopped resets the path; an arrived destination cannot be reused.
                hasMeleeDestination = false;
                rushEndpointReached = true;
            }
            if (!HoldsGround && meleePhase == MeleeMovementPhase.Circle && UsesCirclingSteps &&
                hasMeleeDestination && !movementModule.HasBlockedPath && movementModule.HasArrived())
            {
                float minimum = Mathf.Max(0f, navigation.combatCirclePauseRange.x);
                float maximum = Mathf.Max(minimum, navigation.combatCirclePauseRange.y);
                circlePauseUntil = Time.time + Random.Range(minimum, maximum);
                circlePaused = circlePauseUntil > Time.time;
                movementModule.SetStopped(true);
                movementModule.SetSpeed(0f);
                hasMeleeDestination = false;
                nextMeleePlanAt = 0f;
            }
            if (Time.time < nextMeleePlanAt) return true;
            float elapsed = Mathf.Clamp(Time.time - lastMeleePlanAt, 0f, 0.5f);
            lastMeleePlanAt = Time.time;
            nextMeleePlanAt = Time.time + Mathf.Max(0.1f, (navigation ? navigation.combatReplanInterval : 0.35f));

            if (HoldsGround && (meleePhase == MeleeMovementPhase.Withdraw || meleePhase == MeleeMovementPhase.Circle) &&
                UpdateHoldGroundRecovery(chaseTarget)) return true;

            Vector3 center = chaseTarget.transform.position;
            Vector3 extents = Vector3.zero;
            if (chaseTarget.TryGetCombatBounds(MeleeLayerMask, out Bounds bounds))
            {
                center.x = bounds.center.x;
                center.z = bounds.center.z;
                extents = bounds.extents;
            }
            Vector3 liveForward = FlatDirection(chaseTarget.CombatForward);
            float liveYaw = Mathf.Atan2(liveForward.x, liveForward.z) * Mathf.Rad2Deg;
            sectorYaw = Mathf.MoveTowardsAngle(sectorYaw, liveYaw, (navigation ? navigation.combatFacingFollowSpeed : 120f) * elapsed);
            Vector3 sectorForward = Quaternion.Euler(0f, sectorYaw, 0f) * Vector3.forward;
            Vector3 radial = transform.position - center;
            radial.y = 0f;
            float currentAngle = Vector3.SignedAngle(sectorForward, FlatDirection(radial), Vector3.up);
            float waypointTolerance = navigation ? navigation.combatWaypointTolerance : 0.3f;
            float sampleRadius = navigation ? navigation.combatNavMeshSampleRadius : 1f;
            float approachGap = attackModule.GetApproachClearance(movementModule.AgentRadius,
                navigation ? navigation.combatApproachRangeFraction : 0.75f);
            if (meleePhase == MeleeMovementPhase.Rush && rushEndpointReached)
            {
                // Animated bones and bounds can leave an arrived endpoint outside strike range.
                rushRangeAdjustment = Mathf.Max(0.2f - approachGap, rushRangeAdjustment -
                    attackModule.GetReadyMeleeRangeCorrection(
                        navigation ? navigation.combatApproachRangeFraction : 0.75f));
                rushEndpointReached = false;
            }
            approachGap = Mathf.Max(0.2f, approachGap + rushRangeAdjustment);
            float holdingGap = Mathf.Max(meleeCombatProfile.holdingDistance,
                Mathf.Max(approachGap + 1.5f, movementModule.AgentRadius + 0.3f));
            float currentGap = radial.magnitude - FootprintRadius(extents, FlatDirection(radial));
            float holdingRadius = FootprintRadius(extents, FlatDirection(radial)) + holdingGap;
            float sectorMargin = GetSectorMargin(holdingRadius, waypointTolerance + sampleRadius);

            if (meleePhase == MeleeMovementPhase.Withdraw &&
                ((Time.time - phaseStartedAt >= meleeCombatProfile.minimumWithdrawalTime && currentGap >= holdingGap - 0.5f) ||
                 Time.time - phaseStartedAt >= meleeCombatProfile.withdrawalTimeout))
                SetMeleePhase(MeleeMovementPhase.Circle);
            if (meleePhase == MeleeMovementPhase.Circle && Time.time >= circleUntil && attackModule.HasReadyMeleeDefinition())
                SetMeleePhase(MeleeMovementPhase.Approach);

            // Check live facing as well as the smoothed orbit sector before rushing.
            float liveAngle = Vector3.SignedAngle(liveForward,
                FlatDirection(transform.position - chaseTarget.transform.position), Vector3.up);
            bool readyToRush = attackModule.HasReadyMeleeDefinition();
            bool sectorReady = IsInPreferredSector(currentAngle, 0f) && IsInPreferredSector(liveAngle, 0f);
            UpdateSectorRecovery(currentAngle, liveAngle, readyToRush, Time.time);
            if (meleePhase == MeleeMovementPhase.Rush && (!readyToRush ||
                !allowFallbackAttack && !IsInPreferredSector(liveAngle, 5f)))
                SetMeleePhase(MeleeMovementPhase.Approach);
            if (meleePhase == MeleeMovementPhase.Approach && readyToRush &&
                (allowFallbackAttack || sectorReady))
                SetMeleePhase(MeleeMovementPhase.Rush);

            movementModule.SetAutoBraking(meleePhase != MeleeMovementPhase.Rush &&
                                          (meleePhase != MeleeMovementPhase.Circle || UsesCirclingSteps));
            // Rush needs a tighter arrival tolerance to reach strike range.
            movementModule.SetStoppingDistance(meleePhase == MeleeMovementPhase.Rush
                ? Mathf.Min(waypointTolerance, 0.05f) : waypointTolerance);

            bool circleNeedsReposition = Mathf.Abs(currentGap - holdingGap) >
                Mathf.Max(1f, navigation ? navigation.combatCircleStepDistance : 1.5f) ||
                !allowFallbackAttack && !IsInPreferredSector(liveAngle, 0f);

            float desiredAngle;
            float gap;
            if (meleePhase == MeleeMovementPhase.Rush)
            {
                // Close from the current side so target rotation cannot move the endpoint sideways.
                desiredAngle = currentAngle;
                gap = approachGap;
            }
            else if (meleePhase == MeleeMovementPhase.Withdraw)
            {
                desiredAngle = currentAngle;
                gap = holdingGap;
            }
            else
            {
                if (meleePhase == MeleeMovementPhase.Circle)
                {
                    // Lead the robot so the waypoint cannot reverse before arrival.
                    desiredAngle = UsesCirclingSteps ? currentAngle :
                        GetCirclingAngle(currentAngle, sectorMargin, holdingRadius, waypointTolerance);
                }
                else
                {
                    // Short arc steps avoid cutting through the target on a side switch.
                    desiredAngle = Mathf.MoveTowardsAngle(currentAngle, PreferredAngle(currentAngle, sectorMargin), 25f);
                }
                gap = HoldsGround && meleePhase == MeleeMovementPhase.Approach
                    ? Mathf.Min(holdingGap, Mathf.Max(approachGap, currentGap)) : holdingGap;
            }

            bool lookAt = meleePhase == MeleeMovementPhase.Rush && currentGap <= holdingGap + 1f ||
                          meleePhase == MeleeMovementPhase.Approach && currentGap <= holdingGap + 1f ||
                          (navigation ? navigation.combatFaceTargetWhileRepositioning : true) &&
                          meleePhase != MeleeMovementPhase.Approach && meleePhase != MeleeMovementPhase.Rush;
            if (rotationModule)
            {
                if (lookAt)
                {
                    rotationModule.SetLookTarget(chaseTarget.transform, chaseTarget.AimOffset);
                    rotationModule.SetMode(RotationMode.LookAt);
                }
                else
                {
                    rotationModule.ClearLookTarget();
                    rotationModule.SetMode(RotationMode.Agent);
                }
            }
            if (meleePhase == MeleeMovementPhase.Circle && UsesCirclingSteps)
            {
                // Spacing or sector changes interrupt both pauses and fixed steps.
                if (!circleNeedsReposition && circlePaused && Time.time < circlePauseUntil)
                {
                    nextMeleePlanAt = Mathf.Min(nextMeleePlanAt, circlePauseUntil);
                    return true;
                }
                circlePaused = false;
                if (!circleNeedsReposition && hasMeleeDestination && !movementModule.HasBlockedPath &&
                    !movementModule.HasArrived())
                {
                    float remaining = Vector3.Distance(transform.position, meleeDestination);
                    if (remaining < bestCircleStepDistance - 0.05f)
                    {
                        bestCircleStepDistance = remaining;
                        circleStepProgressAt = Time.time;
                    }
                    if (Time.time - circleStepProgressAt < meleeCombatProfile.blockedSectorGrace) return true;
                    // Local avoidance can stall a complete path; release the step after the grace.
                    hasMeleeDestination = false;
                }
                desiredAngle = GetCirclingAngle(currentAngle, sectorMargin, holdingRadius, waypointTolerance);
            }
            else circlePaused = false;
            MovementProfile speeds = movementModule.MovementProfile;
            float speed = meleePhase == MeleeMovementPhase.Approach || meleePhase == MeleeMovementPhase.Rush
                ? (speeds ? speeds.runMovementSpeed : 2f) :
                meleePhase == MeleeMovementPhase.Withdraw ? (navigation ? navigation.GetCombatWithdrawalSpeed() : 2f) :
                (navigation ? navigation.GetCombatCirclingSpeed() : 2f);

            bool rushNeedsRangeMovement = meleePhase == MeleeMovementPhase.Rush &&
                Mathf.Abs(attackModule.GetReadyMeleeRangeCorrection(
                    navigation ? navigation.combatApproachRangeFraction : 0.75f)) > 0.001f;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                bool fallback = allowFallbackAttack && attempt == 2;
                float candidateGap = fallback && meleePhase == MeleeMovementPhase.Rush ? approachGap : gap;
                float angle;
                if (!fallback && meleePhase == MeleeMovementPhase.Circle && UsesCirclingSteps)
                {
                    // Fixed angular alternatives could turn a short step into a long sidestep.
                    float stepAngle = Mathf.DeltaAngle(currentAngle, desiredAngle);
                    angle = attempt == 1 ? currentAngle - stepAngle :
                        attempt == 2 ? currentAngle + stepAngle * 0.5f : desiredAngle;
                }
                else angle = fallback ? currentAngle :
                    desiredAngle + (attempt == 1 ? 15f : attempt == 2 ? -15f : 0f);
                // Intermediate arc steps may leave the sector; final goals need an interior margin.
                bool interiorGoal = !fallback && IsInPreferredSector(angle, 0f) &&
                    (meleePhase == MeleeMovementPhase.Approach || meleePhase == MeleeMovementPhase.Circle);
                if (interiorGoal)
                    angle = PreferredAngle(angle, sectorMargin);
                else if (!fallback && IsInPreferredSector(currentAngle, 0f) &&
                    meleePhase != MeleeMovementPhase.Withdraw && meleePhase != MeleeMovementPhase.Rush)
                    angle = PreferredAngle(angle);
                Vector3 direction = Quaternion.Euler(0f, sectorYaw + angle, 0f) * Vector3.forward;
                Vector3 candidate = center + direction * (FootprintRadius(extents, direction) + candidateGap);
                if (!movementModule.TrySampleMeleeNavigationPosition(candidate, sampleRadius, out Vector3 point))
                    continue;
                Vector3 sampledRadial = point - center;
                sampledRadial.y = 0f;
                if (sampledRadial.magnitude < FootprintRadius(extents, FlatDirection(sampledRadial)) + candidateGap - 0.75f)
                    continue;
                float sampledAngle = Vector3.SignedAngle(sectorForward, FlatDirection(sampledRadial), Vector3.up);
                if (interiorGoal)
                {
                    Vector3 sampledLiveRadial = point - chaseTarget.transform.position;
                    sampledLiveRadial.y = 0f;
                    float sampledLiveAngle = Vector3.SignedAngle(liveForward,
                        FlatDirection(sampledLiveRadial), Vector3.up);
                    float arrivalMargin = GetSectorMargin(sampledRadial.magnitude, waypointTolerance);
                    float liveArrivalMargin = GetSectorMargin(sampledLiveRadial.magnitude, waypointTolerance);
                    if (!IsInPreferredSector(sampledAngle, -arrivalMargin) ||
                        !IsInPreferredSector(sampledLiveAngle, -liveArrivalMargin)) continue;
                }
                if (!fallback && IsInPreferredSector(currentAngle, 0f) && meleePhase != MeleeMovementPhase.Withdraw &&
                    !IsInPreferredSector(sampledAngle, 5f)) continue;

                bool arrived = hasMeleeDestination && movementModule.HasArrived();
                bool needsSectorRecovery = meleePhase == MeleeMovementPhase.Approach && readyToRush && !sectorReady;
                // Sampling can collapse a Rush goal onto the current position without closing range.
                float rushTolerance = Mathf.Min(waypointTolerance, 0.05f);
                Vector3 horizontalStep = point - transform.position;
                horizontalStep.y = 0f;
                if (rushNeedsRangeMovement && horizontalStep.sqrMagnitude <= rushTolerance * rushTolerance)
                    continue;
                if (meleePhase == MeleeMovementPhase.Circle &&
                    (point - transform.position).sqrMagnitude <= waypointTolerance * waypointTolerance) continue;
                if (meleePhase == MeleeMovementPhase.Circle && UsesCirclingSteps &&
                    Mathf.Abs(currentGap - holdingGap) <= waypointTolerance &&
                    IsInPreferredSector(liveAngle, 0f) &&
                    horizontalStep.magnitude > Mathf.Max(navigation.combatCircleStepDistance,
                        waypointTolerance * 2f) + waypointTolerance) continue;
                // Sector recovery must move beyond an arrived boundary waypoint.
                if (arrived && needsSectorRecovery &&
                    (point - transform.position).sqrMagnitude <= waypointTolerance * waypointTolerance) continue;
                if (hasMeleeDestination && !(arrived && (needsSectorRecovery || meleePhase == MeleeMovementPhase.Circle)) && !movementModule.HasBlockedPath &&
                    (point - meleeDestination).sqrMagnitude < (navigation ? navigation.combatDestinationChangeThreshold : 0.5f) * (navigation ? navigation.combatDestinationChangeThreshold : 0.5f))
                {
                    if (!arrived)
                    {
                        movementModule.SetStopped(false);
                        movementModule.SetSpeed(speed);
                    }
                    return true;
                }
                if (!movementModule.MoveToCompletePath(point)) continue;
                movementModule.SetStopped(false);
                movementModule.SetSpeed(speed);
                meleeDestination = point;
                hasMeleeDestination = true;
                if (meleePhase == MeleeMovementPhase.Circle && UsesCirclingSteps)
                {
                    bestCircleStepDistance = Vector3.Distance(transform.position, point);
                    circleStepProgressAt = Time.time;
                }
                if (!fallback)
                {
                    blockedSince = -1f;
                    if (sectorReady) allowFallbackAttack = false;
                }
                return true;
            }

            if (blockedSince < 0f) blockedSince = Time.time;
            allowFallbackAttack |= Time.time - blockedSince >= meleeCombatProfile.blockedSectorGrace;
            // Fallback relaxes the sector only; attack range and facing still apply.
            movementModule.Stop();
            hasMeleeDestination = false;
            return true;
        }

        private void ResetSectorRecovery()
        {
            sectorRecoverySince = -1f;
            bestSectorError = float.PositiveInfinity;
        }

        private void UpdateSectorRecovery(float currentAngle, float liveAngle, bool readyToRush, float now)
        {
            float error = Mathf.Max(GetSectorError(currentAngle), GetSectorError(liveAngle));
            if (meleePhase != MeleeMovementPhase.Approach || !readyToRush || error <= 0f)
            {
                ResetSectorRecovery();
                return;
            }
            // Complete paths can repeat a boundary endpoint; measure actual sector progress.
            if (sectorRecoverySince < 0f || error < bestSectorError - 0.5f)
            {
                sectorRecoverySince = now;
                bestSectorError = error;
            }
            if (!allowFallbackAttack && now - sectorRecoverySince >= meleeCombatProfile.blockedSectorGrace)
            {
                allowFallbackAttack = true;
                hasMeleeDestination = false;
            }
        }

        private float GetSectorError(float angle)
        {
            if (meleeCombatProfile.positioning == MeleePositioning.Unrestricted) return 0f;
            return meleeCombatProfile.positioning == MeleePositioning.Flank
                ? Mathf.Max(0f, Mathf.Abs(Mathf.Abs(angle) - meleeCombatProfile.flankAngle) - 20f)
                : Mathf.Max(0f, Mathf.Abs(angle) - meleeCombatProfile.frontHalfAngle);
        }

        private static float GetSectorMargin(float radius, float clearance)
        {
            return Mathf.Asin(Mathf.Clamp01(clearance / Mathf.Max(0.1f, radius))) * Mathf.Rad2Deg + 1f;
        }

        private float PreferredAngle(float angle, float margin = 0f)
        {
            if (meleeCombatProfile.positioning == MeleePositioning.Unrestricted) return angle;
            if (meleeCombatProfile.positioning == MeleePositioning.Flank)
            {
                float side = flankSide;
                float lower = meleeCombatProfile.flankAngle - 20f;
                float upper = Mathf.Min(180f, meleeCombatProfile.flankAngle + 20f);
                margin = Mathf.Min(margin, (upper - lower) * 0.45f);
                return side * Mathf.Clamp(side * angle, lower + margin, upper - margin);
            }
            float halfAngle = Mathf.Max(0f, meleeCombatProfile.frontHalfAngle - margin);
            return Mathf.Clamp(angle, -halfAngle, halfAngle);
        }

        private bool IsInPreferredSector(float angle, float padding)
        {
            if (meleeCombatProfile.positioning == MeleePositioning.Unrestricted) return true;
            if (meleeCombatProfile.positioning == MeleePositioning.Flank)
                return Mathf.Abs(Mathf.Abs(angle) - meleeCombatProfile.flankAngle) <= 20f + padding;
            return Mathf.Abs(angle) <= meleeCombatProfile.frontHalfAngle + padding;
        }

        private float GetCirclingAngle(float currentAngle, float margin, float radius, float tolerance)
        {
            float travelSpeed = navigation ? navigation.GetCombatCirclingSpeed() : 2f;
            float angularSpeed = Mathf.Min(navigation ? navigation.combatCircleDegreesPerSecond : 25f,
                travelSpeed / Mathf.Max(0.1f, radius) * Mathf.Rad2Deg);
            float horizon = Mathf.Max(0.5f, 2f * (navigation ? navigation.combatReplanInterval : 0.35f));
            float lead = Mathf.Min(25f, angularSpeed * horizon);
            if (UsesCirclingSteps)
            {
                float distance = Mathf.Max(navigation.combatCircleStepDistance, tolerance * 2f);
                float stepAngle = 2f * Mathf.Asin(Mathf.Clamp01(distance / (2f * Mathf.Max(0.1f, radius)))) * Mathf.Rad2Deg;
                lead = Mathf.Min(lead, stepAngle);
            }
            float projected = currentAngle + circleDirection * lead;
            float goal = PreferredAngle(projected, margin);
            float turnMargin = GetSectorMargin(radius, tolerance) + 1f;
            if (Mathf.Abs(Mathf.DeltaAngle(projected, goal)) > 0.1f &&
                Mathf.Abs(Mathf.DeltaAngle(currentAngle, goal)) <= turnMargin)
            {
                circleDirection *= -1f;
                goal = PreferredAngle(currentAngle + circleDirection * lead, margin);
            }
            return Mathf.MoveTowardsAngle(currentAngle, goal, 25f);
        }

        private static float FootprintRadius(Vector3 extents, Vector3 direction)
        {
            return Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.z) * extents.z;
        }

        private static Vector3 FlatDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
