using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Archura.Blackboard;
using Random = UnityEngine.Random;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StrafeTarget", story: "[Blackboard] strafes around [Target] at distance [StrafeDistance]", category: "Action", id: "s5d1c3c1a2bd18e861799e0cd868ae24")]
    public partial class StrafeTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        private MovementModule movementModule;
        private RotationModule rotationModule;

        private const float MOVE_THRESHOLD_SQR = 0.5f;
        private readonly DestinationThrottle destinationThrottle = new DestinationThrottle();
        private bool strafeRight;

        protected override Status OnStart()
        {
            Blackboard blackboard = Blackboard != null ? Blackboard.Value : null;
            movementModule = blackboard ? blackboard.movementModule : null;
            rotationModule = blackboard ? blackboard.rotationModule : null;
            destinationThrottle.Reset();

            if (movementModule && movementModule.MovementProfile)
                movementModule.SetSpeed(movementModule.MovementProfile.walkMovementSpeed);

            strafeRight = Random.value > 0.5f;

            if (rotationModule && Target.Value != null)
            {
                rotationModule.SetLookTarget(Target.Value.transform);
                rotationModule.SetMode(RotationMode.LookAt);
            }

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
            float distance = movementModule.MovementProfile.strafeDistance;

            if (MovementPointPicker.TryPickStrafePoint(movementModule.transform.position, threatPos.position, distance, strafeRight, out Vector3 strafePoint))
            {
                desiredPoint = strafePoint;
            }
            else
            {
                return Status.Failure;
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
            if (rotationModule)
            {
                rotationModule.ClearLookTarget();
                rotationModule.SetMode(RotationMode.Agent);
            }
        }
    }
}
