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
    [NodeDescription(name: "RunAutonomousCommand", story: "[Blackboard] selects [Subgraph] from [AutonomousCommand]", category: "Action", id: "b78ce08dec12fa7b8664e7b980c9788a")]
    public partial class RunAutonomousCommandAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<AutonomousCommandDefinition> AutonomousCommand;
        [SerializeReference] public BlackboardVariable<BehaviorGraph> Subgraph;

        protected override Status OnStart()
        {
            var blackboard  = Blackboard?.Value;
            var command = AutonomousCommand?.Value;

            if (blackboard == null || command?.subgraph == null)
                return Status.Failure;

            command.subgraph.BlackboardReference.SetVariableValue("Blackboard", blackboard);
            Subgraph.Value = command.subgraph;
            return Status.Success;
        }
    }
}
