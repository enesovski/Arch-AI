using System;
using Unity.Behavior;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;

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
