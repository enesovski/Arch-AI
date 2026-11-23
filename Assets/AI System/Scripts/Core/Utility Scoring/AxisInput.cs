using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Artifika.AI.Utility")]
    public abstract class AxisInput : ScriptableObject
    {
        public abstract float GetNormalizedValue(TacticalContext ctx);
    }
}
