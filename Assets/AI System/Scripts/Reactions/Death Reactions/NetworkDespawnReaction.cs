using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Archura.Death.Reactions
{
    public class NetworkDespawnReaction : NetworkBehaviour, IDeathReaction
    {
        [SerializeField] private bool destroyObject = true;
        [SerializeField] private float destroyAfter = 0f;
        public void Execute()
        {
            if(destroyAfter > 0f) StartCoroutine(ExecuteAfter());
            else Despawn(destroyObject);
        }
        private IEnumerator ExecuteAfter()
        {
            yield return new WaitForSeconds(destroyAfter);
            Despawn(destroyObject);
        }

        private void Despawn(bool destroy)
        {
            if (!IsServer)
                return;

            if (NetworkObject == null)
            {
                ArchuraLog.Error($"[{nameof(NetworkDespawnReaction)}] No NetworkObject found to despawn.", this);
                return;
            }

            NetworkObject.Despawn(destroy);
        }
    }
}
