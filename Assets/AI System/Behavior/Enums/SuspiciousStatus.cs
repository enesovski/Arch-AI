using Unity.Behavior;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [BlackboardEnum]
    public enum SuspiciousStatus
    {
        Alerted,
        Searching,
        None
    }
}
