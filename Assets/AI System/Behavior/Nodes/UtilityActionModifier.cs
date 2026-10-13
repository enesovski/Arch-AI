using System;
using Artifika.AI;
using Artifika.AI.Utility;
using Unity.Behavior;
using UnityEngine;
using Modifier = Unity.Behavior.Modifier;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Utility Action", story: "Scores [Action]", category: "Flow", id: "a6819e0bcb65dd0dd212b9e26150060a")]
public partial class UtilityActionModifier : Modifier, IUtilityActionNode
{

    [SerializeReference]
    public BlackboardVariable<UtilityActionDefinition> Action;

    public float EvaluateScore(TacticalContext ctx)
    {
        if (Action?.Value == null)
            return 0f;

        return Action.Value.EvaluateScore(ctx);
    }

    public string GetActionName()
    {
        return Action?.Value != null ? Action.Value.ActionName : "Unnamed Action";
    }

    protected override Status OnStart()
    {
        if (Child != null)
        {
            return StartNode(Child);
        }
        return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Child == null)
            return Status.Failure;

        return Child.CurrentStatus;
    }
}



