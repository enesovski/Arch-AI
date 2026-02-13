using Unity.Netcode;
using UnityEngine;

namespace Artifika.AI.Death.Reactions
{
    public class NetworkDespawnReaction : NetworkDespawner, IDeathReaction
    {
        [SerializeField] private bool destroyObject = true;
        public void Initialize(Blackboard blackboard)
        {
        }

        public void Execute()
        {
            Despawn(destroyObject);
        }
    }
}