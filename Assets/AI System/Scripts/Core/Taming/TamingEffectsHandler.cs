using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class TamingEffectsHandler : MonoBehaviour
    {
        [SerializeField] private GameObject tamingEffectPrefab;
        [SerializeField] private float destroyDelay = 5f;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe<EntityTamedEvent>(OnEntityTamed);
        }

        private void OnDisable()
        {
            EventManager.Instance?.Unsubscribe<EntityTamedEvent>(OnEntityTamed);
        }

        private void OnEntityTamed(EntityTamedEvent evt)
        {
            if (tamingEffectPrefab == null) return;

            GameObject effectInstance = Instantiate(tamingEffectPrefab, evt.Position, Quaternion.identity);

            var mmfPlayer = effectInstance.GetComponent<MMF_Player>();
            if (mmfPlayer != null)
            {
                mmfPlayer.PlayFeedbacks();
            }

            Destroy(effectInstance, destroyDelay);
        }
    }
}
