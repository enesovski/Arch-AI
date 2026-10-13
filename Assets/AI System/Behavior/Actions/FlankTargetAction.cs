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
    [NodeDescription(name: "FlankTarget", story: "[Blackboard] flanks [Target] at distance [FlankDistance]", category: "Action", id: "f2c1c3c1a2bd18e861799e0cd868ae23")]
    public partial class FlankTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        private MovementModule movementModule;

        private const float MOVE_THRESHOLD = 0.5f;
        private Vector3 lastDestination = Vector3.positiveInfinity;

        protected override Status OnStart()
        {
            movementModule = Blackboard.Value.movementModule;

            if (movementModule)
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

            if ((lastDestination - desiredPoint).sqrMagnitude > MOVE_THRESHOLD)
            {
                movementModule.MoveTo(desiredPoint);
                lastDestination = desiredPoint;
            }

            if (movementModule.HasArrived())
            {
                return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            movementModule?.Stop();
        }
    }
}
