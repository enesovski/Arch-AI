using UnityEngine;

namespace Artifika.AI.Utility
{
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
