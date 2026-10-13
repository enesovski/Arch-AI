using System;
using Artifika.AI;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Blackboard = Artifika.AI.Blackboard;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StrafeTarget", story: "[Blackboard] strafes around [Target] at distance [StrafeDistance]", category: "Action", id: "s5d1c3c1a2bd18e861799e0cd868ae24")]
public partial class StrafeTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    [SerializeReference] public BlackboardVariable<GameEntity> Target;
    
    private MovementModule movementModule;
    private RotationModule rotationModule;

    private const float MOVE_THRESHOLD = 0.5f;
    private Vector3 lastDestination = Vector3.positiveInfinity;
    private bool strafeRight;

    protected override Status OnStart()
    {
        var blackboard = Blackboard != null ? Blackboard.Value : null;
        movementModule = blackboard != null ? blackboard.movementModule : null;
        rotationModule = blackboard != null ? blackboard.rotationModule : null;
        lastDestination = Vector3.positiveInfinity;
        
        if (movementModule)
            movementModule.SetSpeed(movementModule.MovementProfile.walkMovementSpeed);

        strafeRight = Random.value > 0.5f;

        if (rotationModule && Target.Value != null)
        {
            rotationModule.SetLookTarget(Target.Value.transform);
            rotationModule.SetMode(RotationMode.LookAt);
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        GameEntity targetEntity = Target.Value;

        if (!targetEntity)
            return Status.Success;

        if (!movementModule)
            return Status.Failure;

        Transform threatPos = targetEntity.transform;
        if (!threatPos)
            return Status.Failure;

        Vector3 desiredPoint;
        float distance = movementModule.MovementProfile.strafeDistance;

        if (MovementPointPicker.TryPickStrafePoint(movementModule.transform.position, threatPos.position, distance, strafeRight, out Vector3 strafePoint))
        {
            desiredPoint = strafePoint;
        }
        else
        {
            return Status.Failure; 
        }

        if ((lastDestination - desiredPoint).sqrMagnitude > MOVE_THRESHOLD)
        {
            movementModule.MoveTo(desiredPoint);
            lastDestination = desiredPoint;
        }

        if (movementModule.HasArrived())
        {
            // Pick a new direction next time we start
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        lastDestination = Vector3.positiveInfinity;
        movementModule?.Stop();
        if (rotationModule)
        {
            rotationModule.ClearLookTarget();
            rotationModule.SetMode(RotationMode.Agent);
        }
    }
}
