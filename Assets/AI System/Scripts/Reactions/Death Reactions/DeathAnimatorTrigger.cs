using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathAnimatorTrigger : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTrigger = "Death";

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
            healthComponent = GetComponent<HealthComponent>() ?? GetComponentInChildren<HealthComponent>(true);

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
