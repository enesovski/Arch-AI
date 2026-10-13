using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public interface ICompanionReaction : IReaction
    {
        void OnCompanionStateChanged(CompanionState previousState, CompanionState nextState);
    }
}
