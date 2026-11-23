using UnityEngine;

namespace Archura
{
    public abstract class ReactionBusBase : MonoBehaviour, IReactionBus
    {
        public virtual void Initialize(Blackboard blackboard) { }
    }
}
