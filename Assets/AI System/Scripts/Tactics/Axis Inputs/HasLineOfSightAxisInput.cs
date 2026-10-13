using UnityEngine;

namespace Artifika.AI.Utility
{
    [CreateAssetMenu(
        fileName = "Axis_HasLineOfSight",
        menuName = "AI/Utility/Axis/Has Line Of Sight")]
    public class HasLineOfSightAxisInput : BoolAxisInput
    {
        public override bool GetBoolValue(TacticalContext ctx)
        {
            return ctx.HasLineOfSight;
        }
    }
}
