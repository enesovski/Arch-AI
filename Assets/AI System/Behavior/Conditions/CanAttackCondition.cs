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
        var bb = Blackboard != null ? Blackboard.Value : null;

        if (bb == null || bb.attackModule == null)
        {
            Debug.LogWarning("CanAttackCondition: Blackboard or AttackModule is not assigned.", this);
            return false;
        }

        return bb.attackModule.CanAttack();
    }

    public override void OnStart()
    {
        
    }

    public override void OnEnd()
    {
        
    }
}
