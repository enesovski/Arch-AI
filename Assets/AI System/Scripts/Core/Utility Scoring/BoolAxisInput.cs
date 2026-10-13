using UnityEngine;

namespace Artifika.AI.Utility
{

    public abstract class BoolAxisInput : AxisInput
    {
        public abstract bool GetBoolValue(TacticalContext ctx);

        public sealed override float GetNormalizedValue(TacticalContext ctx)
            => GetBoolValue(ctx) ? 1f : 0f;
    }
}