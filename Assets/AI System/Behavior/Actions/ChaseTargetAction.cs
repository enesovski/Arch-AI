using System;
using Artifika.AI.Attack;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChaseTarget", story: "[Blackboard] chases [Target]", category: "Action", id: "4a1c3c1a2bd18e861799e0cd868ae237")]
public partial class ChaseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    [SerializeReference] public BlackboardVariable<GameEntity> Target;
    
    private MovementModule _movementModule;
    private AttackModule _attackModule;

    private const float DestinationThresholdSqr = 0.01f;
    private Vector3 _lastIssuedDestination = Vector3.positiveInfinity;

    protected override Status OnStart()
    {
        _movementModule = Blackboard.Value.movementModule;
        _attackModule = Blackboard.Value.attackModule;
        
        if (_movementModule)
            _movementModule.SetSpeed(_movementModule.MovementProfile.chaseSpeed);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        MovementModule movementModule = _movementModule;
        AttackModule attackModule = _attackModule;
        GameEntity targetEntity = Target.Value;

        if (!targetEntity)
            return Status.Success;

        if (!movementModule || !attackModule)
            return Status.Failure;

        if (attackModule.CanAttack())
        {
            movementModule.Stop();
            return Status.Success;
        }

        Transform threatPos = targetEntity.transform;
        if (!threatPos)
            return Status.Failure;

        Vector3 desiredPoint;

        if (MovementPointPicker.TryPickChasePoint(
                movementModule.transform,
                threatPos,
                movementModule.MovementProfile.flankAngleMax,
                out Vector3 chasePoint))
        {
            desiredPoint = chasePoint;
        }
        else
        {
            desiredPoint = threatPos.position;
        }

        if ((_lastIssuedDestination - desiredPoint).sqrMagnitude > DestinationThresholdSqr)
        {
            movementModule.MoveTo(desiredPoint);
            _lastIssuedDestination = desiredPoint;
        }
        else
        {
            movementModule.MoveTo(desiredPoint); 
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _movementModule?.Stop();
    }


}

