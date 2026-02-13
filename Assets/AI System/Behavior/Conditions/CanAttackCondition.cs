using System;
using Unity.Behavior;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can Attack", story: "[Blackboard] can attack", category: "Conditions", id: "9f32bf90013148e3a5dd6468fc5659a1")]
public partial class CanAttackCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    public override bool IsTrue()
    {
        return Blackboard.Value.attackModule.CanAttack();
    }

    public override void OnStart()
    {
        
    }

    public override void OnEnd()
    {
        
    }
}
