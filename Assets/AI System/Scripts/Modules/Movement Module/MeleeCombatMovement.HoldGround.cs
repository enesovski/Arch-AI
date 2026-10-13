using UnityEngine;

namespace Artifika.AI
{
    internal sealed partial class MeleeCombatMovement
    {
        private Vector3 withdrawalStart, withdrawalDirection;
        private float withdrawalDistance;
        private bool withdrawalFinished;
        private Vector3 holdGroundAnchor, holdGroundTangent;
        private float holdGroundTargetDistance;

        private bool UpdateHoldGroundRecovery(GameEntity target)
        {
            movementModule.SetAutoBraking(true);
            float tolerance = navigation ? navigation.combatWaypointTolerance : 0.3f;
            movementModule.SetStoppingDistance(tolerance);
            if (rotationModule)
            {
                if (!navigation || navigation.combatFaceTargetWhileRepositioning)
                {
                    rotationModule.SetLookTarget(target.transform, target.AimOffset);
                    rotationModule.SetMode(RotationMode.LookAt);
                }
                else rotationModule.SetMode(RotationMode.Agent);
            }

            if (meleePhase == MeleeMovementPhase.Withdraw)
            {
                if (Time.time - phaseStartedAt >= meleeCombatProfile.withdrawalTimeout ||
                    withdrawalFinished && Time.time - phaseStartedAt >= meleeCombatProfile.minimumWithdrawalTime)
                    EnterHoldGround(target);
                else if (withdrawalFinished)
                    return true;
                else if (hasMeleeDestination)
                {
                    if (movementModule.HasArrived())
                    {
                        withdrawalFinished = true;
                        StopHoldGroundMovement();
                        return true;
                    }
                    if (movementModule.HasBlockedPath || LocalStepStalled())
                        EnterHoldGround(target);
                    else return true;
                }
                else
                {
                    // This world-space origin and direction were captured once at attack end.
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        float distance = withdrawalDistance * (attempt == 0 ? 1f : attempt == 1 ? 0.5f : 0.25f);
                        Vector3 candidate = withdrawalStart + withdrawalDirection * distance;
                        if (TryStartLocalStep(candidate, withdrawalStart, withdrawalDistance,
                                navigation ? navigation.GetCombatWithdrawalSpeed() : 2f)) return true;
                    }
                    EnterHoldGround(target); // No space behind us: stand here instead of taking a detour.
                }
            }

            bool recoveryElapsed = Time.time >= circleUntil;
            bool pushed = HorizontalDistance(target.transform.position, holdGroundAnchor) < holdGroundTargetDistance - 0.5f;
            if ((recoveryElapsed || pushed) && attackModule.HasReadyMeleeDefinition())
            {
                if (attackModule.HasReadyMeleeAttackInRange())
                {
                    float angle = Vector3.SignedAngle(FlatDirection(target.CombatForward),
                        FlatDirection(transform.position - target.transform.position), Vector3.up);
                    // A boss holding its ground must not walk a large orbit just to get back into view.
                    if (recoveryElapsed && Time.time - phaseStartedAt >= meleeCombatProfile.blockedSectorGrace)
                        allowFallbackAttack = true;
                    Vector3 toTarget = FlatDirection(target.AimPosition - transform.position);
                    bool facing = Vector3.Dot(FlatDirection(transform.forward), toTarget) >=
                                  Mathf.Cos(meleeCombatProfile.attackFacingHalfAngle * Mathf.Deg2Rad);
                    StopHoldGroundMovement();
                    if ((allowFallbackAttack || IsInPreferredSector(angle, 5f)) && facing)
                        SetMeleePhase(MeleeMovementPhase.Rush);
                    return true; // The graph starts the attack using the normal cooldown/range/facing checks.
                }
                if (recoveryElapsed)
                {
                    StopHoldGroundMovement();
                    SetMeleePhase(MeleeMovementPhase.Approach);
                    return false;
                }
            }

            if (hasMeleeDestination)
            {
                if (!movementModule.HasArrived() && !movementModule.HasBlockedPath && !LocalStepStalled())
                    return true;
                circleDirection = -circleDirection;
                PauseHoldGroundSteps();
                return true;
            }
            if (circlePaused && Time.time < circlePauseUntil)
            {
                nextMeleePlanAt = Mathf.Min(nextMeleePlanAt, circlePauseUntil);
                return true;
            }
            circlePaused = false;
            float halfStep = Mathf.Max(0f, navigation ? navigation.combatCircleStepDistance : 1f) * 0.5f;
            // Alternate on a short fixed strip, without moving the anchor when the player advances.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Vector3 candidate = holdGroundAnchor + holdGroundTangent * (halfStep * circleDirection);
                if (TryStartLocalStep(candidate, holdGroundAnchor, halfStep,
                        navigation ? navigation.GetCombatCirclingSpeed() : 1.5f)) return true;
                circleDirection = -circleDirection;
            }
            PauseHoldGroundSteps();
            return true;
        }

        private void EnterHoldGround(GameEntity target)
        {
            StopHoldGroundMovement();
            SetMeleePhase(MeleeMovementPhase.Circle);
            holdGroundAnchor = transform.position;
            holdGroundTangent = Vector3.Cross(Vector3.up, FlatDirection(target.transform.position - holdGroundAnchor));
            holdGroundTargetDistance = HorizontalDistance(target.transform.position, holdGroundAnchor);
        }

        private bool TryStartLocalStep(Vector3 candidate, Vector3 anchor, float radius, float speed)
        {
            float tolerance = navigation ? navigation.combatWaypointTolerance : 0.3f;
            if (radius <= tolerance) return false;
            float sampleRadius = Mathf.Min(0.5f, navigation ? navigation.combatNavMeshSampleRadius : 0.5f);
            if (!movementModule.TrySampleMeleeNavigationPosition(candidate, sampleRadius, out Vector3 point) ||
                HorizontalDistance(point, anchor) > radius + 0.01f ||
                HorizontalDistance(point, transform.position) <= tolerance ||
                !movementModule.HasStraightNavigationTo(point) || !movementModule.MoveToCompletePath(point)) return false;
            meleeDestination = point;
            hasMeleeDestination = true;
            circleStepProgressAt = Time.time;
            bestCircleStepDistance = Vector3.Distance(transform.position, point);
            movementModule.SetStopped(false);
            movementModule.SetSpeed(speed);
            return true;
        }

        private bool LocalStepStalled()
        {
            float remaining = Vector3.Distance(transform.position, meleeDestination);
            if (remaining < bestCircleStepDistance - 0.05f)
            {
                bestCircleStepDistance = remaining;
                circleStepProgressAt = Time.time;
            }
            return Time.time - circleStepProgressAt >= meleeCombatProfile.blockedSectorGrace;
        }

        private void StopHoldGroundMovement()
        {
            movementModule.SetStopped(true);
            movementModule.SetSpeed(0f);
            hasMeleeDestination = false;
        }

        private void PauseHoldGroundSteps()
        {
            StopHoldGroundMovement();
            float minimum = UsesCirclingSteps ? Mathf.Max(0f, navigation.combatCirclePauseRange.x) : 0f;
            float maximum = UsesCirclingSteps ? Mathf.Max(minimum, navigation.combatCirclePauseRange.y) : minimum;
            circlePauseUntil = Time.time + Random.Range(minimum, maximum);
            circlePaused = circlePauseUntil > Time.time;
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            Vector3 delta = first - second;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
