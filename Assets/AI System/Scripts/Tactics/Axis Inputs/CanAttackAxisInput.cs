using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Artifika.AI.Utility")]
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
