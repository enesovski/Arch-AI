using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    [Serializable]
    public class MandatoryCondition
    {
        [HorizontalGroup(Width = 0.6f)]
        [HideLabel]
        public AxisInput input;

        [HorizontalGroup]
        [LabelText("Min")]
        [Range(0f, 1f)]
        public float threshold = 0.5f;

        public bool Passes(TacticalContext ctx)
            => input != null && input.GetNormalizedValue(ctx) >= threshold;
    }

    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "UtilityActionDefinition", menuName = "AI/Utility Action Definition")]
    public class UtilityActionDefinition : ScriptableObject
    {
        [HideLabel]
        [SerializeField] private string actionName;

        [Title("Gates")]
        [Tooltip("ALL conditions must pass their threshold for this action to be scored. Any failure returns 0.")]
        [SerializeField] private List<MandatoryCondition> mandatoryConditions = new();

        [Title("Scoring")]
        [LabelText("Multiplier")]
        [Tooltip("Global score multiplier. Use to bias this action relative to others without touching curves.")]
        [Range(0f, 2f)]
        [SerializeField] private float multiplier = 1f;

        [SerializeField] private List<UtilityAxis> axes = new();

        [LabelText("Random Variance")]
        [Tooltip("Adds a slight random nudge to prevent perfectly deterministic behavior.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float randomVariance = 0.05f;

        public string ActionName => string.IsNullOrWhiteSpace(actionName) ? name : actionName;

        public float EvaluateScore(TacticalContext ctx)
        {
            foreach (MandatoryCondition condition in mandatoryConditions)
            {
                if (!condition.Passes(ctx))
                    return 0f;
            }

            if (axes == null || axes.Count == 0)
                return multiplier;

            float score = 1f;

            foreach (UtilityAxis axis in axes)
            {
                if (axis == null)
                    continue;

                score *= axis.Evaluate(ctx);
            }

            score *= multiplier;

            if (randomVariance > 0f)
            {
                score += UnityEngine.Random.Range(-randomVariance, randomVariance);
                score = Mathf.Max(0f, score);
            }

            return score;
        }

#if UNITY_EDITOR
        public override string ToString() => ActionName;
#endif
    }
}
