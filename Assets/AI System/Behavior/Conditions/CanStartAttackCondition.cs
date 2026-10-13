using System;
using Unity.Behavior;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can Start Attack", story: "[Blackboard] can start attack", category: "Conditions", id: "b4dce9f5808e4924a3cb7d0dd44d412d")]
public partial class CanStartAttackCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    public override bool IsTrue()
    {
        var blackboard = Blackboard != null ? Blackboard.Value : null;
        var attackModule = blackboard != null ? blackboard.attackModule : null;
        return attackModule != null && attackModule.CanStartAttack();
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
