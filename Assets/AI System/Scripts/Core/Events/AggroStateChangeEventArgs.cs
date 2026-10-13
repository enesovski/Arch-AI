using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public readonly struct AggroStateChangeEventArgs
    {
        public AggroState PreviousState { get; }
        public AggroState NewState { get; }

        public AggroStateChangeEventArgs(
            AggroState previousState,
            AggroState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}
