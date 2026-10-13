using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Flee From Target", story: "[Blackboard] flees from [Target]", category: "Action", id: "6c6f1c56738af7ae4cc3699784c17004")]
    public partial class FleeFromTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        private const float MinFleeDistance = 30f;
        private const float FleeDistanceIntervalMultiplier = 10f;
        private const float FleeSpeedLookaheadSeconds = 3f;
        private const float RetryCooldown = 0.75f;
        private const float SafetyReevaluationInterval = 1.5f;
        private const float DirectionDriftThreshold = 60f;

        private MovementModule movementModule;
        private Blackboard blackboard;
        private Transform threat;

        private Vector3 fleeDestination;
        private Vector3 lastFleeDirection;
        private float preferredSide;
        private float nextRetryTime;
        private float nextSafetyCheckTime;
        private bool hasDestination;
        private bool restoreAutoBraking;

        protected override Status OnStart()
        {
            blackboard = Blackboard?.Value;
            movementModule = blackboard?.movementModule;
            threat = Target?.Value ? Target.Value.transform : null;

            if (movementModule == null || threat == null)
                return Status.Failure;

            preferredSide = 0f;
            hasDestination = false;
            restoreAutoBraking = movementModule.AutoBraking;
            movementModule.SetAutoBraking(false);

            PickNextFleeDestination();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (movementModule == null)
                return Status.Failure;

            if (threat == null)
                return Status.Success;

            if (NeedsNewDestination())
                PickNextFleeDestination();

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (movementModule == null)
                return;

            movementModule.SetAutoBraking(restoreAutoBraking);

            if (!IsStillFleeing())
                movementModule.Stop();
        }

        private bool NeedsNewDestination()
        {
            if (!hasDestination)
                return Time.time >= nextRetryTime;

            if (movementModule.HasArrived())
                return true;

            if (movementModule.HasBlockedPath)
                return true;

            if (movementModule.IsStuck())
                return true;

            return Time.time >= nextSafetyCheckTime && HasFleeDirectionDrifted();
        }

        private bool HasFleeDirectionDrifted()
        {
            Vector3 currentAwayDir = movementModule.transform.position - threat.position;
            currentAwayDir.y = 0f;

            if (currentAwayDir.sqrMagnitude < 0.01f)
                return false;

            return Vector3.Angle(lastFleeDirection, currentAwayDir.normalized) > DirectionDriftThreshold;
        }

        private void PickNextFleeDestination()
        {
            MovementProfile profile = movementModule.MovementProfile;
            if (profile == null)
            {
                FailPick();
                return;
            }

            Vector3 agentPos = movementModule.transform.position;
            Vector3 threatPos = threat.position;
            float fleeSpeed = CalculateFleeSpeed(profile);
            float fleeDistance = CalculateFleeDistance(profile, fleeSpeed);

            Vector3 awayDir = agentPos - threatPos;
            awayDir.y = 0f;
            lastFleeDirection = awayDir.sqrMagnitude > 0.01f ? awayDir.normalized : Vector3.forward;

            if (!MovementPointPicker.TryPickFleePoint(agentPos, threatPos, fleeDistance, preferredSide, out fleeDestination, out float chosenSide))
            {
                FailPick();
                return;
            }

            preferredSide = chosenSide;
            movementModule.SetSpeed(fleeSpeed);

            if (movementModule.MoveTo(fleeDestination))
            {
                hasDestination = true;
                nextSafetyCheckTime = Time.time + SafetyReevaluationInterval;
            }
            else
            {
                FailPick();
            }
        }

        private void FailPick()
        {
            movementModule.Stop();
            hasDestination = false;
            nextRetryTime = Time.time + RetryCooldown;
        }

        private static float CalculateFleeSpeed(MovementProfile profile)
        {
            return profile.runMovementSpeed > 0f
                ? profile.runMovementSpeed
                : profile.walkMovementSpeed;
        }

        private static float CalculateFleeDistance(MovementProfile profile, float fleeSpeed)
        {
            float intervalDistance = profile.runDistanceInterval * FleeDistanceIntervalMultiplier;
            float speedLookaheadDistance = fleeSpeed * FleeSpeedLookaheadSeconds;
            return Mathf.Max(intervalDistance, speedLookaheadDistance, MinFleeDistance);
        }

        private bool IsStillFleeing()
        {
            return blackboard != null
                   && blackboard.aggroModule != null
                   && blackboard.aggroModule.CurrentAggroState == AggroState.Alerted
                   && Target?.Value != null;
        }
    }
}
