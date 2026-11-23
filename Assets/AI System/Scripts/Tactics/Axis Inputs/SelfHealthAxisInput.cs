using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Artifika.AI.Utility")]
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
