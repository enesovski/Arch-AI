using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public sealed class DeathAnimatorTrigger : MonoBehaviour
    {
        [Tooltip("Component implementing " + nameof(IEntityHealth) + ". Resolved from the hierarchy when empty.")]
        [FormerlySerializedAs("healthComponent")]
        [SerializeField] private MonoBehaviour healthSource;
        [SerializeField] private Animator animator;
        [SerializeField] private string deathTrigger = "Death";

        private IEntityHealth healthComponent;
        private int deathTriggerHash;
        private bool subscribed;

        private void Awake()
        {
            deathTriggerHash = Animator.StringToHash(deathTrigger);
            ResolveTargets();
        }

        private void OnEnable()
        {
            ResolveTargets();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveTargets()
        {
            if (healthComponent == null)
                healthComponent = healthSource as IEntityHealth
                                  ?? GetComponent<IEntityHealth>() ?? GetComponentInChildren<IEntityHealth>(true);

            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        }

        private void Subscribe()
        {
            if (subscribed || healthComponent == null)
                return;

            healthComponent.OnDeath += TriggerDeath;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || healthComponent == null)
                return;

            healthComponent.OnDeath -= TriggerDeath;
            subscribed = false;
        }

        private void TriggerDeath()
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
                return;

            animator?.SetTrigger(deathTriggerHash);
        }
    }
}
