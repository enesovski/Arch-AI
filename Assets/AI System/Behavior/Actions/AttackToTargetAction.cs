using System;
using Artifika.AI.Attack;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackToTarget", story: "[Blackboard] attacks to Target", category: "AI", id: "12f0e122f0a182b5eb420fad6d5802fe")]
public partial class AttackToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    private AttackModule _attackModule;
    private bool _attackStarted;

    protected override Status OnStart()
    {
        _attackModule = Blackboard.Value.attackModule;
        _attackStarted = false;
        
        if (!_attackModule)
            return Status.Failure;
        
        if (!_attackModule.canAttack)
            return Status.Failure;

        _attackModule.Attack();
        _attackStarted = true;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_attackStarted)
            return Status.Failure;
        
        if (!_attackModule.canAttack)
            return Status.Running;

        return Status.Success;
    }

    protected override void OnEnd()
    {
        _attackStarted = false;
    }
}

