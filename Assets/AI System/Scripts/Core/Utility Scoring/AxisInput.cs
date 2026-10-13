using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Utility
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI.Utility", sourceAssembly: "Assembly-CSharp")]
    public abstract class AxisInput : ScriptableObject
    {
        public abstract float GetNormalizedValue(TacticalContext ctx);
    }
}
