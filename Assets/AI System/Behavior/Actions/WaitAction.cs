using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard  = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "WaitAction", story: "[Blackboard] waits in place", category: "Action", id: "69ea848b87ba4b0fe2ef52b2ac48e43e")]
public partial class WaitAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    protected override Status OnStart()
    {
        var bb = Blackboard?.Value;
        if (bb != null && bb.movementModule != null)
        {
            bb.movementModule.Stop();
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Running; 
    }
}

