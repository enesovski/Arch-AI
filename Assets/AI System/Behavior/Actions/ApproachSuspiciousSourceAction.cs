using System;
using Artifika.AI.Aggro;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Approach Suspicious Source",
        story: "[Blackboard] approaches suspicious source",
        category: "Action",
        id: "c7ec7d4a8f534c19a5f239553810a207")]
    public partial class ApproachSuspiciousSourceAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        private MovementModule movementModule;
        private AggroModule aggroModule;
        private RotationModule rotationModule;
        private Vector3 lastDestination = Vector3.positiveInfinity;

        private const float DestinationRefreshDistance = 0.75f;

        protected override Status OnStart()
        {
            if (Blackboard?.Value == null)
                return Status.Failure;

            movementModule = Blackboard.Value.movementModule;
            aggroModule = Blackboard.Value.aggroModule;
            rotationModule = Blackboard.Value.rotationModule;

            if (movementModule == null || aggroModule == null || movementModule.MovementProfile == null)
                return Status.Failure;

            movementModule.SetSpeed(GetApproachSpeed());
            rotationModule?.SetMode(RotationMode.Agent);
            lastDestination = Vector3.positiveInfinity;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (aggroModule.CurrentSuspiciousState != SuspiciousState.Alerted)
                return Status.Success;

            float stopDistance = GetStopDistance();
            Vector3 source = aggroModule.LastKnownPosition;
            Vector3 position = movementModule.transform.position;

            if (Vector3.Distance(position, source) <= stopDistance)
            {
                movementModule.Stop();
                return Status.Success;
            }

            Vector3 toSource = source - position;
            toSource.y = 0f;

            if (toSource.sqrMagnitude < 0.01f)
                return Status.Success;

            Vector3 destination = source - toSource.normalized * stopDistance;

            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                return Status.Success;

            if ((lastDestination - hit.position).sqrMagnitude > DestinationRefreshDistance * DestinationRefreshDistance)
            {
                movementModule.SetSpeed(GetApproachSpeed());
                movementModule.MoveTo(hit.position);
                lastDestination = hit.position;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            movementModule?.Stop();
            rotationModule?.SetMode(RotationMode.Agent);
        }

        private float GetApproachSpeed()
        {
            MovementProfile profile = movementModule.MovementProfile;

            if (profile.searchMovementSpeed > 0f)
                return profile.searchMovementSpeed;

            if (profile.walkMovementSpeed > 0f)
                return profile.walkMovementSpeed;

            return movementModule.CalculateMinSpeed();
        }

        private float GetStopDistance()
        {
            if (aggroModule.configuration != null)
                return aggroModule.configuration.suspiciousApproachStopDistance;

            return 7f;
        }
    }
}
