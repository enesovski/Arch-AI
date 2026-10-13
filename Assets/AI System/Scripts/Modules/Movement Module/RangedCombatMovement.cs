using Artifika.AI.Attack;
using UnityEngine;

namespace Artifika.AI
{
    public enum RangedMovementPhase { Approach, Retreat, Reposition, Aim, Attack }

    // Owned by MovementModule and ticked by the ranged graph.
    internal sealed class RangedCombatMovement
    {
        private readonly MovementModule movement;
        private readonly Blackboard blackboard;
        private AttackModule attack => blackboard.attackModule;
        private RangedCombatProfile profile => movement.RangedCombatProfile;
        private MovementProfile navigation => movement.MovementProfile;
        private Transform transform => movement.transform;
        public RangedMovementPhase Phase { get; private set; } = RangedMovementPhase.Aim;

        private GameEntity target;
        private bool active;
        private bool hasDestination;
        private bool savedBraking;
        private float savedStoppingDistance;
        private RotationMode savedRotation;
        private Vector3 destination;
        private Vector3 plannedTargetPosition;
        private Vector3 retreatStart;
        private Vector3 retreatDirection;
        private float nextPlanAt;
        private float phaseDeadline;
        private float aimUntil;
        private float retreatRetryAt;
        private float approachRetryAt;
        private float rejectUntil;
        private Vector3 rejectedDestination;
        private float bestDistance;
        private float progressAt;
        private float side = 1f;

        public RangedCombatMovement(MovementModule movement, Blackboard blackboard)
        {
            this.movement = movement;
            this.blackboard = blackboard;
        }

        private bool SetTarget(GameEntity newTarget)
        {
            if (!newTarget) return false;
            if (target == newTarget) return true;
            Reset();
            target = newTarget;
            side = Random.value < 0.5f ? -1f : 1f;
            EnterAim();
            return true;
        }

        private void Begin()
        {
            if (active) return;
            savedBraking = movement.AutoBraking;
            savedStoppingDistance = movement.StoppingDistance;
            savedRotation = blackboard.rotationModule ? blackboard.rotationModule.CurrentMode : RotationMode.Agent;
            movement.SetAutoBraking(true);
            movement.SetStoppingDistance(Mathf.Clamp(navigation.combatWaypointTolerance, 0.05f, 0.5f));
            if (Phase == RangedMovementPhase.Aim)
            {
                movement.Stop();
                movement.SetStopped(true);
            }
            active = true;
        }

        public void End()
        {
            if (!active) return;
            movement.Stop();
            movement.SetAutoBraking(savedBraking);
            movement.SetStoppingDistance(savedStoppingDistance);
            if (blackboard.rotationModule && !attack.IsAttackCommitted)
            {
                blackboard.rotationModule.ClearLookTarget();
                blackboard.rotationModule.SetMode(savedRotation);
            }
            active = false;
            hasDestination = false;
        }

        public void Reset()
        {
            End();
            target = null;
            Phase = RangedMovementPhase.Aim;
            hasDestination = false;
            nextPlanAt = aimUntil = retreatRetryAt = approachRetryAt = rejectUntil = 0f;
        }

        public bool IsPositionReady(GameEntity newTarget)
        {
            if (!SetTarget(newTarget)) return false;
            float distance = FlatDistance(transform.position, target.transform.position);
            return Phase == RangedMovementPhase.Aim && Time.time >= aimUntil &&
                   (distance >= Mathf.Max(0f, profile.retreatTriggerDistance) || Time.time < retreatRetryAt);
        }

        public void BeginRecovery(GameEntity newTarget)
        {
            if (!SetTarget(newTarget)) return;
            side = -side;
            EnterMotion(RangedMovementPhase.Reposition);
        }

        public void BeginAttack(GameEntity newTarget)
        {
            if (SetTarget(newTarget))
            {
                Phase = RangedMovementPhase.Attack;
                hasDestination = false;
            }
        }

        public bool Update(GameEntity newTarget)
        {
            if (!movement.IsServer || !SetTarget(newTarget)) return false;
            if (blackboard.healthComponent && !blackboard.healthComponent.IsAlive)
            {
                Reset();
                return true;
            }
            if (!movement.CanNavigate) return true;
            Begin();
            if (attack.IsAttackCommitted)
            {
                Phase = RangedMovementPhase.Attack;
                return true;
            }
            if (Phase == RangedMovementPhase.Attack) EnterAim();
            if (blackboard.rotationModule)
            {
                blackboard.rotationModule.SetLookTarget(target.transform, target.AimOffset);
                blackboard.rotationModule.SetMode(RotationMode.LookAt);
            }

            float distance = FlatDistance(transform.position, target.transform.position);
            if (Phase != RangedMovementPhase.Retreat && Time.time >= retreatRetryAt &&
                distance < Mathf.Max(0f, profile.retreatTriggerDistance))
            {
                // Capture once: a chasing player must not keep extending this retreat.
                retreatStart = transform.position;
                retreatDirection = FlatDirection(transform.position - target.transform.position);
                EnterMotion(RangedMovementPhase.Retreat);
            }
            else if (Phase == RangedMovementPhase.Aim && Time.time >= approachRetryAt && distance > profile.MaximumDistance + 0.5f)
                EnterMotion(RangedMovementPhase.Approach);

            if (Phase == RangedMovementPhase.Aim)
            {
                if (Time.time >= nextPlanAt)
                {
                    nextPlanAt = Time.time + PlanInterval;
                    if (!attack.HasRangedShotAt(transform.position, target))
                        EnterMotion(RangedMovementPhase.Reposition);
                }
                if (Phase == RangedMovementPhase.Aim) return true;
            }

            if (Phase == RangedMovementPhase.Approach && distance <= profile.MaximumDistance - 0.5f &&
                attack.HasRangedShotAt(transform.position, target))
            {
                EnterAim();
                return true;
            }

            if (hasDestination)
            {
                float remaining = FlatDistance(transform.position, destination);
                if (movement.HasArrived())
                {
                    if (Phase != RangedMovementPhase.Approach || distance <= profile.MaximumDistance + 0.5f)
                    {
                        FinishMotion();
                        return true;
                    }
                    hasDestination = false;
                    nextPlanAt = 0f;
                }
                else if (remaining < bestDistance - 0.1f)
                {
                    bestDistance = remaining;
                    progressAt = Time.time;
                }
                else if (Time.time - progressAt >= 1.2f)
                {
                    // Local avoidance can stall even a complete path.
                    if (Phase == RangedMovementPhase.Retreat)
                    {
                        FinishMotion();
                        return true;
                    }
                    if (Phase == RangedMovementPhase.Approach && attack.HasRangedShotAt(transform.position, target))
                    {
                        YieldApproach();
                        return true;
                    }
                    rejectedDestination = destination;
                    rejectUntil = Time.time + PlanInterval * 2f;
                    hasDestination = false;
                    movement.Stop();
                    nextPlanAt = 0f;
                }
            }

            if (Phase != RangedMovementPhase.Approach && Time.time >= phaseDeadline)
            {
                FinishMotion();
                return true;
            }

            bool targetMoved = Phase == RangedMovementPhase.Approach &&
                FlatDistance(plannedTargetPosition, target.transform.position) >=
                Mathf.Max(0.5f, navigation.combatDestinationChangeThreshold);
            if ((!hasDestination || targetMoved) && Time.time >= nextPlanAt)
            {
                nextPlanAt = Time.time + PlanInterval;
                if (!TryPlan() && !hasDestination)
                {
                    movement.Stop();
                    if (Phase == RangedMovementPhase.Retreat) FinishMotion();
                    else if (attack.HasRangedShotAt(transform.position, target))
                    {
                        if (Phase == RangedMovementPhase.Approach) YieldApproach();
                        else FinishMotion();
                    }
                }
            }
            return true;
        }

        private float PlanInterval => Mathf.Max(0.1f, navigation.combatReplanInterval);

        private void YieldApproach()
        {
            // Allow a usable shot while preferred spacing is unreachable.
            approachRetryAt = Time.time + 2f;
            EnterAim();
        }

        private void EnterMotion(RangedMovementPhase phase)
        {
            movement.Stop();
            Phase = phase;
            hasDestination = false;
            nextPlanAt = 0f;
            phaseDeadline = Time.time + Mathf.Max(0.2f, profile.repositionTimeout);
        }

        private void FinishMotion()
        {
            if (Phase == RangedMovementPhase.Retreat)
                retreatRetryAt = Time.time + 2f;
            EnterAim();
        }

        private void EnterAim()
        {
            movement.Stop();
            movement.SetStopped(true);
            Phase = RangedMovementPhase.Aim;
            hasDestination = false;
            aimUntil = Time.time + Mathf.Max(0f, profile.aimPause);
            nextPlanAt = Time.time + PlanInterval;
        }

        private bool TryPlan()
        {
            // A second pass permits a search step when every shooting position is blocked.
            int passes = Phase == RangedMovementPhase.Retreat ? 1 : 2;
            for (int pass = 0; pass < passes; pass++)
            for (int i = 0; i < 6; i++)
            {
                Vector3 candidate = Candidate(i);
                if (!movement.TrySampleMeleeNavigationPosition(candidate,
                        Mathf.Max(0.1f, navigation.combatNavMeshSampleRadius), out Vector3 sampled)) continue;
                if (FlatDistance(sampled, transform.position) <= Mathf.Max(0.5f, movement.StoppingDistance + 0.1f)) continue;
                if (Time.time < rejectUntil && FlatDistance(sampled, rejectedDestination) < 0.5f) continue;
                if (Phase == RangedMovementPhase.Retreat)
                {
                    if (FlatDistance(sampled, retreatStart) > Mathf.Max(0.1f, navigation.rangedRetreatStepDistance) + 0.25f ||
                        FlatDistance(sampled, target.transform.position) <= FlatDistance(transform.position, target.transform.position) + 0.25f ||
                        !movement.HasStraightNavigationTo(sampled)) continue;
                }
                else if (pass == 0 && !attack.HasRangedShotAt(sampled, target)) continue;

                movement.SetStopped(false);
                float speed = Phase switch
                {
                    RangedMovementPhase.Approach => navigation.runMovementSpeed,
                    RangedMovementPhase.Retreat => navigation.rangedRetreatSpeed > 0f
                        ? navigation.rangedRetreatSpeed : navigation.runMovementSpeed,
                    _ => navigation.rangedRepositionSpeed > 0f
                        ? navigation.rangedRepositionSpeed : navigation.walkMovementSpeed
                };
                movement.SetSpeed(speed);
                if (!movement.MoveToCompletePath(sampled)) continue;
                destination = sampled;
                plannedTargetPosition = target.transform.position;
                bestDistance = FlatDistance(transform.position, destination);
                progressAt = Time.time;
                hasDestination = true;
                return true;
            }
            return false;
        }

        private Vector3 Candidate(int index)
        {
            if (Phase == RangedMovementPhase.Retreat)
            {
                float retreatAngle = index switch
                {
                    1 => 45f * side,
                    2 => -45f * side,
                    3 => 75f * side,
                    4 => -75f * side,
                    _ => 0f
                };
                float step = Mathf.Max(0.1f, navigation.rangedRetreatStepDistance) * (index == 5 ? 0.5f : 1f);
                return retreatStart + Quaternion.Euler(0f, retreatAngle, 0f) * retreatDirection * step;
            }

            Vector3 center = target.transform.position;
            Vector3 radial = FlatDirection(transform.position - center);
            float radius = Phase == RangedMovementPhase.Approach ? (profile.MinimumDistance + profile.MaximumDistance) * 0.5f :
                Mathf.Clamp(FlatDistance(transform.position, center), profile.MinimumDistance, profile.MaximumDistance);
            float angleStep = Mathf.Atan2(Mathf.Max(0.1f, navigation.rangedRepositionStepDistance), radius) * Mathf.Rad2Deg;
            float angle = Phase == RangedMovementPhase.Approach
                ? index switch
                {
                    0 => 0f,
                    1 => angleStep * side,
                    2 => -angleStep * side,
                    3 => 45f * side,
                    4 => -45f * side,
                    _ => 90f * side
                }
                : index switch
                {
                    0 => angleStep * side,
                    1 => -angleStep * side,
                    2 => 2f * angleStep * side,
                    3 => -2f * angleStep * side,
                    4 => angleStep * 0.5f * side,
                    _ => -angleStep * 0.5f * side
                };
            Vector3 result = center + Quaternion.Euler(0f, angle, 0f) * radial * radius;
            result.y = transform.position.y;
            return result;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }
        private Vector3 FlatDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return -transform.forward;
            return direction.normalized;
        }
    }
}
