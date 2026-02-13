using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flee From Target", story: "[Blackboard] flees from [Target]", category: "Action", id: "6c6f1c56738af7ae4cc3699784c17004")]
public partial class FleeFromTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    [SerializeReference] public BlackboardVariable<GameEntity> Target;
    
    private MovementModule _movementModule;
    private Vector3 fleeDestination;

    protected override Status OnStart()
    {
        MovementModule movementModule = Blackboard.Value.movementModule;
        Transform threat = Target.Value.transform;

        Vector3 agentPos = movementModule.transform.position;
        Vector3 threatPos = threat.transform.position;
        float fleeDistance = movementModule.MovementProfile.fleeDistance;
        movementModule.SetSpeed(movementModule.MovementProfile.fleeSpeed);

        if (MovementPointPicker.TryPickFleePoint(agentPos, threatPos, fleeDistance, out fleeDestination))
        {
            movementModule.MoveTo(fleeDestination);
        }

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

    }
}

