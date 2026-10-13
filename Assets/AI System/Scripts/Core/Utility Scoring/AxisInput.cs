using UnityEngine;

namespace Artifika.AI.Utility
{
    public abstract class AxisInput : ScriptableObject
    {
        public abstract float GetNormalizedValue(TacticalContext ctx);
    }
}