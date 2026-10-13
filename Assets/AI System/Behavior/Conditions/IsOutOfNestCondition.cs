using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Is Out Of Nest Radius", story: "[Blackboard] is out of nest", category: "Conditions", id: "467a93e782f91997930121088b46b6cd")]
    public partial class IsOutOfNestCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        private MovementModule movementModule;
        private float arriveTolerance = 0.5f;
        private float nestRadius;
        public override bool IsTrue()
        {
            movementModule = Blackboard.Value.movementModule;
            nestRadius = Blackboard.Value.nestRadius;
            Vector3 nest = movementModule.SpawnPoint;
            float sqrDist = (movementModule.transform.position - nest).sqrMagnitude;
            if (nestRadius > 0f && sqrDist <= (nestRadius + arriveTolerance) * (nestRadius + arriveTolerance))
            {
                return false;
            }
            return true;
        }

        public override void OnStart()
        {
        }

        public override void OnEnd()
        {
        }
    }
}
