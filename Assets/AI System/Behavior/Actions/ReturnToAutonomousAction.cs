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
    [NodeDescription(name: "ReturnToAutonomous", story: "[Blackboard] returns to autonomous state", category: "Action", id: "6a58413463d52a46d72d66e0e87f967e")]
    public partial class ReturnToAutonomousAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        protected override Status OnStart()
        {
            Blackboard?.Value?.commandModule.RestorePreviousAutonomousCommand();
            return Status.Success;
        }
    }
}
