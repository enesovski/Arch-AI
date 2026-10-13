using UnityEngine;

namespace Artifika.AI.Utility
{
    [CreateAssetMenu(
        fileName = "Axis_TimeSinceLastAttack",
        menuName = "AI/Utility/Axis/Time Since Last Attack")]
    public class TimeSinceLastAttackAxisInput : AxisInput
    {
        [SerializeField, Min(0.01f)] private float maxTimeSinceLastAttack = 3f;

        public override float GetNormalizedValue(TacticalContext ctx)
        {
            return Mathf.Clamp01(ctx.TimeSinceLastAttack / maxTimeSinceLastAttack);
        }
    }
}
