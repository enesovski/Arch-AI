using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
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
