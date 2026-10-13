using UnityEngine;

namespace Artifika.AI
{
    public abstract class ReactionBusBase : MonoBehaviour, IReactionBus
    {
        public virtual void Initialize(Blackboard blackboard) { }
    }
}
