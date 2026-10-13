using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsStunned", story: "[Blackboard] is stunned", category: "Conditions", id: "a13d269e24f421cd6c97481635655958")]
    public partial class IsStunnedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        public override bool IsTrue()
        {
            return Blackboard.Value != null && Blackboard.Value.IsStunned();
        }

        public override void OnStart()
        {
        }

        public override void OnEnd()
        {
        }
    }
}
