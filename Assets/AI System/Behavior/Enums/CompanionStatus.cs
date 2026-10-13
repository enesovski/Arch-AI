using Unity.Behavior;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [BlackboardEnum]
    public enum CompanionStatus
    {
        Autonomous,
        Commanded,
        Mounted,
        Disabled
    }
}
