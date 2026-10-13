using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    public abstract class BoolAxisInput : AxisInput
    {
        public abstract bool GetBoolValue(TacticalContext ctx);

        public sealed override float GetNormalizedValue(TacticalContext ctx)
            => GetBoolValue(ctx) ? 1f : 0f;
    }
}
