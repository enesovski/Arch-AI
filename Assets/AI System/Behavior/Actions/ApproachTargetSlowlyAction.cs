using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Approach Target Slowly",
    story: "[Blackboard] approaches [Target] slowly",
    category: "Action",
    id: "b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7")]
public partial class ApproachTargetSlowlyAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    [SerializeReference] public BlackboardVariable<GameEntity> Target;

    [SerializeReference] public BlackboardVariable<float> StandoffDistance;

    private MovementModule movementModule;


    private const float MOVE_THRESHOLD = 0.01f;
    private Vector3 lastDestination = Vector3.positiveInfinity;

    protected override Status OnStart()
    {
        movementModule = Blackboard.Value.movementModule;
        movementModule.SetSpeed(movementModule.MovementProfile.walkMovementSpeed);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!Target.Value)
            return Status.Success;

        float standoff = StandoffDistance.Value > 0f ? StandoffDistance.Value : 4f;
        float distance = Vector3.Distance(movementModule.transform.position, Target.Value.transform.position);

        if (distance <= standoff)
        {
            movementModule.Stop();
            return Status.Running;
        }

        // Pick a point that stops at standoff range, not directly on the target.
        Vector3 dir = (Target.Value.transform.position - movementModule.transform.position).normalized;
        Vector3 destination = Target.Value.transform.position - dir * standoff;

        if ((lastDestination - destination).sqrMagnitude > MOVE_THRESHOLD)
        {
            movementModule.MoveTo(destination);
            lastDestination = destination;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        movementModule?.Stop();
    }
}