using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
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
