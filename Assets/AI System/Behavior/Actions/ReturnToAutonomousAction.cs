using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;
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

