using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;
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

