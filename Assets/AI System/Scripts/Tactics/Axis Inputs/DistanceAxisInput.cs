using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(
        fileName = "Axis_Distance",
        menuName = "AI/Utility/Axis/Distance")]
    public class DistanceAxisInput : AxisInput
    {
        [SerializeField, Min(0.01f)] private float maxDistance = 20f;

        public override float GetNormalizedValue(TacticalContext ctx)
        {
            return Mathf.Clamp01(ctx.Distance / maxDistance);
        }
    }
}
