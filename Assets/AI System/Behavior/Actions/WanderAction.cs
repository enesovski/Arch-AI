using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wander", story: "[Blackboard] wanders around", category: "Action", id: "f4e530ae0e735ce5976221ce18e4c039")]
public partial class WanderAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    private MovementModule _movementModule;
    protected override Status OnStart()
    {
        var bb = Blackboard != null ? Blackboard.Value : null;
        if (bb == null || bb.movementModule == null)
        {
            Debug.LogWarning("WanderAction: Blackboard or MovementModule is not assigned.", this);
            return Status.Failure;
        }

        _movementModule = bb.movementModule;

        MovementProfile movementData = _movementModule.MovementProfile;
        if (movementData == null)
        {
            Debug.LogWarning("WanderAction: MovementProfile is not assigned on MovementModule.", this);
            return Status.Failure;
        }
        Transform transform = _movementModule.transform;

        Vector3 nest = _movementModule.SpawnPoint;
        bool ok = MovementPointPicker.TryPickWanderPoint(
            transform,
            nest,
            movementData.nestRadius,
            movementData.minWanderDistance,
            movementData.maxWanderDistance,
            out Vector3 wanderTarget);

        if (!ok)
            return Status.Failure;

        _movementModule.SetSpeed(_movementModule.MovementProfile.wanderSpeed);

        // If the server-side MoveTo fails (no valid path), fail the node instead of leaving it running.
        if (!_movementModule.MoveTo(wanderTarget))
            return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_movementModule.HasArrived())
            return Status.Success;
        return Status.Running;

    }

    protected override void OnEnd()
    {
        _movementModule?.Stop();
    }
}

