using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    [Serializable]
    public class UtilityAxis
    {
        [HorizontalGroup(Width = 0.35f)]
        [HideLabel]
        [SerializeField] private AxisInput input;

        [HorizontalGroup]
        [HideLabel]
        [SerializeField] private AnimationCurve responseCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [HorizontalGroup(Width = 0.18f)]
        [LabelText("Weight")]
        [Range(0f, 2f)]
        [SerializeField] private float weight = 1f;

        public float Evaluate(TacticalContext ctx)
        {
            if (input == null)
                return 1f;

            float value = responseCurve.Evaluate(input.GetNormalizedValue(ctx));
            return Mathf.Max(0f, 1f + (value - 1f) * weight);
        }

#if UNITY_EDITOR
        public override string ToString()
        {
            return input != null ? input.name : "(no input)";
        }
#endif
    }
}
