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
    [NodeDescription(name: "Wander", story: "[Blackboard] wanders around", category: "Action", id: "f4e530ae0e735ce5976221ce18e4c039")]
    public partial class WanderAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        private MovementModule movementModule;
        protected override Status OnStart()
        {
            movementModule = Blackboard.Value.movementModule;

            MovementProfile movementData = movementModule.MovementProfile;
            Transform transform = movementModule.transform;

            Vector3 nest = movementModule.SpawnPoint;
            bool ok = MovementPointPicker.TryPickWanderPoint(
                transform,
                nest,
                Blackboard.Value.nestRadius,
                movementData.MinWanderDistance,
                movementData.MaxWanderDistance,
                out Vector3 wanderTarget);

            if (!ok)
                return Status.Failure;

            movementModule.SetSpeed(movementModule.MovementProfile.walkMovementSpeed);
            movementModule.MoveTo(wanderTarget);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (movementModule.HasArrived())
                return Status.Success;
            return Status.Running;
        }

        protected override void OnEnd()
        {
            movementModule?.Stop();
        }
    }
}
