using Unity.Behavior;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [BlackboardEnum]
    public enum CompanionStatus
    {
        Autonomous,
        Commanded,
        Mounted,
        Disabled
    }
}
