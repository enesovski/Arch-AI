using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Is Dead", story: "[Blackboard] is dead", category: "Conditions", id: "bc490b19dd8287173d0aadab2b638275")]
    public partial class IsDeadCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        public override bool IsTrue()
        {
            return !Blackboard.Value.IsAlive();
        }

        public override void OnStart()
        {
        }

        public override void OnEnd()
        {
        }
    }
}
