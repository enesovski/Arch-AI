using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public interface ICompanionReaction : IReaction
    {
        void OnCompanionStateChanged(CompanionState previousState, CompanionState nextState);
    }
}
