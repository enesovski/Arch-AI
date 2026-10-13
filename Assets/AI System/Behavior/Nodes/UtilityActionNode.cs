using Sirenix.OdinInspector;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Artifika.AI.Utility
{
    public interface IUtilityActionNode
    {
        float EvaluateScore(TacticalContext ctx);
        string GetActionName();
    }

    [Serializable]
    public abstract class UtilityActionNode : Action, IUtilityActionNode
    {
        [Title("Utility Scoring")]
        [Tooltip("Reference to a UtilityActionDefinition ScriptableObject that defines the axes and score for this action.")]
        [SerializeField] private UtilityActionDefinition actionDefinition;

        public float EvaluateScore(TacticalContext ctx)
        {
            if (actionDefinition == null)
                return 0f;

            return actionDefinition.EvaluateScore(ctx);
        }

        public string GetActionName()
        {
            return actionDefinition != null ? actionDefinition.ActionName : GetType().Name;
        }
    }
}