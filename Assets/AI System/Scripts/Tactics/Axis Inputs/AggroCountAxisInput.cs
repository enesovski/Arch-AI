using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(
        fileName = "Axis_AggroCount",
        menuName = "AI/Utility/Axis/Aggro Count")]
    public class AggroCountAxisInput : AxisInput
    {
        [SerializeField, Min(1)] private int maxAggroCount = 3;

        public override float GetNormalizedValue(TacticalContext ctx)
        {
            return Mathf.Clamp01((float)ctx.TargetAggroCount / maxAggroCount);
        }
    }
}
