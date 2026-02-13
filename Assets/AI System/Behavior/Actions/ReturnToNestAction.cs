using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReturnToNest", story: "[Blackboard] returns to nest", category: "Action", id: "55b1f21d303ce65be0d8896a39b46217")]
public partial class ReturnToNestAction : Action
{

    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    
    private MovementModule _movementModule;
    private float _nestRadius;
    
    private const float ArriveTolerance = 0.5f;
    protected override Status OnStart()
    {

        _movementModule = Blackboard.Value.movementModule;
        Vector3 nest = _movementModule.SpawnPoint;

        _nestRadius = Blackboard.Value.nestRadius;
        float sqrDist = (_movementModule.transform.position - nest).sqrMagnitude;

        if (_nestRadius > 0f && sqrDist <= (_nestRadius + ArriveTolerance) * (_nestRadius + ArriveTolerance))
        {
            return Status.Success;
        }

        _movementModule.SetStopped(false);
        _movementModule.SetSpeed(_movementModule.MovementProfile.chaseSpeed);
        _movementModule.MoveTo(nest);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_movementModule.HasArrived())
            return Status.Success;

        Vector3 nest = _movementModule.SpawnPoint;
        float sqrDist = (_movementModule.transform.position - nest).sqrMagnitude;

        if (_nestRadius > 0f)
        {
            float threshold = _nestRadius + ArriveTolerance;
            if (sqrDist <= threshold * threshold)
                return Status.Success;
        }
        else
        {
            if (sqrDist <= ArriveTolerance * ArriveTolerance)
                return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

