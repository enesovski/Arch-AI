using System;
using Unity.Behavior;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Has Line Of Sight", story: "[Blackboard] has line of sight", category: "Conditions", id: "8f0f432d8f584cc88d31b92f95c21b10")]
public partial class HasLineOfSightCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    public override bool IsTrue()
    {
        var blackboard = Blackboard != null ? Blackboard.Value : null;
        return blackboard != null && blackboard.GetTacticalContext().HasLineOfSight;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
