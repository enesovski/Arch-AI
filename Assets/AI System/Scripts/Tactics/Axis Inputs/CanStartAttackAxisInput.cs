using UnityEngine;

namespace Artifika.AI.Utility
{
    [CreateAssetMenu(
        fileName = "Axis_CanStartAttack",
        menuName = "AI/Utility/Axis/Can Start Attack")]
    public class CanStartAttackAxisInput : BoolAxisInput
    {
        public override bool GetBoolValue(TacticalContext ctx)
        {
            return ctx.CanStartAttack;
        }
    }
}
