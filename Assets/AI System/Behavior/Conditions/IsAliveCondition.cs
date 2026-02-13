using System;
using Unity.Behavior;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Alive", story: "[Blackboard] is dead", category: "Conditions", id: "76626eb4bfc7e768445de5efe460cc36")]
public partial class IsAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    public override bool IsTrue()
    {
        return !Blackboard.Value.healthComponent.IsAlive;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
