using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
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
