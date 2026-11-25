using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Archura.Blackboard;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Approach Target Slowly",
        story: "[Blackboard] approaches [Target] slowly",
        category: "Action",
        id: "b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7")]
    public partial class ApproachTargetSlowlyAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        [SerializeReference] public BlackboardVariable<float> StandoffDistance;

        private MovementModule movementModule;

        private const float MOVE_THRESHOLD_SQR = 0.01f;
        private readonly DestinationThrottle destinationThrottle = new DestinationThrottle();

        protected override Status OnStart()
        {
            Blackboard blackboard = Blackboard != null ? Blackboard.Value : null;
            movementModule = blackboard ? blackboard.movementModule : null;
            destinationThrottle.Reset();

            if (movementModule && movementModule.MovementProfile)
                movementModule.SetSpeed(movementModule.MovementProfile.walkMovementSpeed);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (!Target.Value)
                return Status.Success;

            if (!movementModule)
                return Status.Failure;

            float standoff = StandoffDistance.Value > 0f ? StandoffDistance.Value : 4f;
            float distance = Vector3.Distance(movementModule.transform.position, Target.Value.transform.position);

            if (distance <= standoff)
            {
                movementModule.Stop();
                return Status.Running;
            }

            // Pick a point that stops at standoff range, not directly on the target.
            Vector3 dir = (Target.Value.transform.position - movementModule.transform.position).normalized;
            Vector3 destination = Target.Value.transform.position - dir * standoff;

            if (destinationThrottle.ShouldMoveTo(destination, MOVE_THRESHOLD_SQR))
                movementModule.MoveTo(destination);

            return Status.Running;
        }

        protected override void OnEnd()
        {
            destinationThrottle.Reset();
            if (movementModule)
                movementModule.Stop();
        }
    }
}
