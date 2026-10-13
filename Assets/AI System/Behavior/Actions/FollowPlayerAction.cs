using Artifika.AI;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FollowPlayer", story: "[Blackboard] follows player", category: "Action", id: "46abcaf7b6b584341e0bd236346ca964")]
public partial class FollowPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    private MovementModule movementModule;
    private MovementProfile movementProfile;
    private Transform ownerTransform;

    private const float ReissueThresholdSqr = 2.25f;
    private Vector3 lastDestination = Vector3.positiveInfinity;

    protected override Status OnStart()
    {
        var blackboard = Blackboard?.Value;
        if (blackboard == null || !blackboard.movementModule || !blackboard.gameEntity?.owner) return Status.Failure;

        movementModule = blackboard.movementModule;
        ownerTransform = blackboard.gameEntity.owner.transform;
        movementProfile = movementModule.MovementProfile;

        if (movementProfile == null) return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ownerTransform || !movementModule) return Status.Failure;

        Vector3 selfPos = movementModule.transform.position;
        Vector3 ownerPos = ownerTransform.position;
        float distSqr = (selfPos - ownerPos).sqrMagnitude;

        float teleportSqr = movementProfile.teleportRadius * movementProfile.teleportRadius;
        float runSqr = movementProfile.followRadius * movementProfile.followRadius;
        float stopSqr = movementProfile.stopRadius * movementProfile.stopRadius;

        if (distSqr > teleportSqr)
        {
            if (movementModule.IsPositionOnNavMesh(ownerPos))
            {
                if (movementModule.Teleport(ownerPos))
                {
                    lastDestination = Vector3.positiveInfinity;
                }
            }
            else
            {
                movementModule.Stop();
                lastDestination = ownerPos; 
            }
            
            return Status.Running;
        }

        if (distSqr <= stopSqr)
        {
            if (lastDestination != Vector3.positiveInfinity)
            {
                movementModule.Stop();
                lastDestination = Vector3.positiveInfinity;
            }
            return Status.Running;
        }

        float targetSpeed = distSqr > runSqr ? movementProfile.runMovementSpeed : movementProfile.walkMovementSpeed;
        movementModule.SetSpeed(targetSpeed);

        if ((lastDestination - ownerPos).sqrMagnitude > ReissueThresholdSqr)
        {
            bool hasContinuousPath = movementModule.HasContinuousPathTo(ownerPos);

            if (hasContinuousPath)
            {
                movementModule.MoveTo(ownerPos);
                lastDestination = ownerPos;
            }
            else
            {
                if (movementModule.IsPositionOnNavMesh(ownerPos))
                {
                    if (movementModule.Teleport(ownerPos))
                    {
                        lastDestination = Vector3.positiveInfinity;
                    }
                }
                else
                {
                    movementModule.Stop();
                    lastDestination = ownerPos;
                }
            }
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        movementModule?.Stop();
        lastDestination = Vector3.positiveInfinity;
    }
}