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
    [NodeDescription(name: "ReturnToNest", story: "[Blackboard] returns to nest", category: "Action", id: "55b1f21d303ce65be0d8896a39b46217")]
    public partial class ReturnToNestAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        private MovementModule movementModule;
        private float nestRadius;

        private const float ArriveTolerance = 0.5f;
        protected override Status OnStart()
        {
            movementModule = Blackboard.Value.movementModule;
            Vector3 nest = movementModule.SpawnPoint;

            nestRadius = Blackboard.Value.nestRadius;
            float sqrDist = (movementModule.transform.position - nest).sqrMagnitude;

            if (nestRadius > 0f && sqrDist <= (nestRadius + ArriveTolerance) * (nestRadius + ArriveTolerance))
            {
                return Status.Success;
            }

            movementModule.SetStopped(false);
            movementModule.SetSpeed(movementModule.MovementProfile.runMovementSpeed);
            movementModule.MoveTo(nest);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (movementModule.HasArrived())
                return Status.Success;

            Vector3 nest = movementModule.SpawnPoint;
            float sqrDist = (movementModule.transform.position - nest).sqrMagnitude;

            if (nestRadius > 0f)
            {
                float threshold = nestRadius + ArriveTolerance;
                if (sqrDist <= threshold * threshold)
                    return Status.Success;
            }
            else
            {
                if (sqrDist <= ArriveTolerance * ArriveTolerance)
                    return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
        }
    }
}
