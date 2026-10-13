using UnityEngine;

namespace Artifika.AI.Utility
{
    [CreateAssetMenu(
        fileName = "Axis_CanAttack",
        menuName = "AI/Utility/Axis/Can Attack")]
    public class CanAttackAxisInput : BoolAxisInput
    {
        public override bool GetBoolValue(TacticalContext ctx)
        {
            return ctx.CanAttack;
        }
    }
}
