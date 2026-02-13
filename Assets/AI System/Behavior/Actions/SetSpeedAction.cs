using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Speed", story: "Set [Blackboard] speed to [Float]", category: "Action", id: "5786eafe995b4451915d661f8b6bb9ef")]
public partial class SetSpeedAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    [SerializeReference] public BlackboardVariable<float> Float;

    protected override Status OnStart()
    {
        Blackboard.Value.movementModule.SetSpeed(Float.Value);
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
        
    }
}

