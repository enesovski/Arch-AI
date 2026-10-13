using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Is Attack Committed", story: "[Blackboard] is attacking", category: "Conditions", id: "aa40f73e90f5400585a6fa49013c7f29")]
    public partial class IsAttackCommittedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        public override bool IsTrue()
        {
            var blackboard = Blackboard != null ? Blackboard.Value : null;
            var attackModule = blackboard != null ? blackboard.attackModule : null;
            bool isCommitted = attackModule != null && attackModule.IsAttackCommitted;

            if (isCommitted)
                blackboard.movementModule?.Stop();

            return isCommitted;
        }

        public override void OnStart()
        {
        }

        public override void OnEnd()
        {
        }
    }
}
