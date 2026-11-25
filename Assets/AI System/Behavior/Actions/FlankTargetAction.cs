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
    [NodeDescription(name: "FlankTarget", story: "[Blackboard] flanks [Target] at distance [FlankDistance]", category: "Action", id: "f2c1c3c1a2bd18e861799e0cd868ae23")]
    public partial class FlankTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        private MovementModule movementModule;

        private const float MOVE_THRESHOLD_SQR = 0.5f;
        private readonly DestinationThrottle destinationThrottle = new DestinationThrottle();

        protected override Status OnStart()
        {
            Blackboard blackboard = Blackboard != null ? Blackboard.Value : null;
            movementModule = blackboard ? blackboard.movementModule : null;
            destinationThrottle.Reset();

            if (movementModule && movementModule.MovementProfile)
                movementModule.SetSpeed(movementModule.MovementProfile.runMovementSpeed);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            GameEntity targetEntity = Target.Value;

            if (!targetEntity)
                return Status.Success;

            if (!movementModule)
                return Status.Failure;

            Transform threatPos = targetEntity.transform;
            if (!threatPos)
                return Status.Failure;

            Vector3 desiredPoint;
            float distance = movementModule.MovementProfile.flankDistance;

            if (MovementPointPicker.TryPickFlankPoint(movementModule.transform.position, threatPos.position, distance, out Vector3 flankPoint))
            {
                desiredPoint = flankPoint;
            }
            else
            {
                desiredPoint = threatPos.position;
            }

            if (destinationThrottle.ShouldMoveTo(desiredPoint, MOVE_THRESHOLD_SQR))
                movementModule.MoveTo(desiredPoint);

            if (movementModule.HasArrived())
            {
                return Status.Success;
            }

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
