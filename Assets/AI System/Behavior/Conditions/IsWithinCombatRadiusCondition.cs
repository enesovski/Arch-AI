using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Is Within Combat Radius", story: "[Blackboard] is within combat radius", category: "Conditions", id: "a82d59c2e3f24e839ca3e2f2168d6a42")]
    public partial class IsWithinCombatRadiusCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        public override bool IsTrue()
        {
            var blackboard = Blackboard != null ? Blackboard.Value : null;
            var attackModule = blackboard != null ? blackboard.attackModule : null;
            return attackModule != null && attackModule.IsTargetWithinCombatRadius();
        }

        public override void OnStart()
        {
        }

        public override void OnEnd()
        {
        }
    }
}
