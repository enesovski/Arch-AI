using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Should Use Attack Branch", story: "[Blackboard] should use attack branch", category: "Conditions", id: "efad8d6ade32443ea10282fdf687d580")]
    public partial class ShouldUseAttackBranchCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        public override bool IsTrue()
        {
            var blackboard = Blackboard != null ? Blackboard.Value : null;
            var attackModule = blackboard != null ? blackboard.attackModule : null;
            return attackModule != null && attackModule.ShouldUseAttackBranch();
        }

        public override void OnStart()
        {
        }

        public override void OnEnd()
        {
        }
    }
}
