using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Artifika.AI;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class EntityCoordinator : NetworkBehaviour
{
    [Title("Update Settings")]
    [SerializeField] private float updateInterval = 0.3f;
    [SerializeField] private Blackboard blackboard;
    public float GetUpdateInterval() => updateInterval;

    private HealthComponent healthComponent;
    private readonly List<IUpdatableEntityModule> registeredModules = new List<IUpdatableEntityModule>();
    private Coroutine updateCoroutine;

    private WaitForSeconds cachedWait;

    public override void OnNetworkSpawn()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer)
            return;

        blackboard?.Initialize();

        foreach (var module in GetComponents<IEntityModule>())
            module?.Initialize();

        healthComponent = blackboard?.healthComponent;

        cachedWait = new WaitForSeconds(updateInterval);
        updateCoroutine = StartCoroutine(UpdateCycle());

        healthComponent.OnDeath += StopUpdateCoroutine;
    }
    
    private IEnumerator UpdateCycle()
    {
        while (true)
        {
            foreach (var t in registeredModules)
            {
                t.PerformUpdate(updateInterval);
            }

            yield return cachedWait;
        }
    }

    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    public void RegisterModule(IUpdatableEntityModule module)
    {
        if (module == null)
            return;

        if (!registeredModules.Contains(module))
        {
            registeredModules.Add(module);
        }
    }

    public void UnregisterModule(IUpdatableEntityModule module)
    {
        if (module == null) return;

        if (registeredModules.Contains(module))
        {
            registeredModules.Remove(module);
        }
    }

    private void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }

        registeredModules.Clear();
    }
}
