using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(
        fileName = "Axis_SelfHealth",
        menuName = "AI/Utility/Axis/Self Health")]
    public class SelfHealthAxisInput : AxisInput
    {
        public override float GetNormalizedValue(TacticalContext ctx)
        {
            return ctx.OwnHealthNormalized;
        }
    }
}
