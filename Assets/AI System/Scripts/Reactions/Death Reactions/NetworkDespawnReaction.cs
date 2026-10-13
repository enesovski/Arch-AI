using System.Collections;
using UnityEngine;

namespace Artifika.AI.Death.Reactions
{
    public class NetworkDespawnReaction : NetworkDespawner, IDeathReaction
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
    }
}
