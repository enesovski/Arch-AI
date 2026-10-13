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
    [SerializeField] protected Blackboard blackboard;
    public float GetUpdateInterval() => updateInterval;

    private HealthComponent healthComponent;
    private readonly List<IUpdatableEntityModule> registeredModules = new();
    private Coroutine updateCoroutine;

    private WaitForSeconds waitForSeconds;
    private readonly HashSet<object> updateLocks = new();

    // Multiplier applied by the SimulationDistance system so entities far from
    // every player think less often. 1 = full rate, and it is clamped there —
    // this may only ever remove work, never add it.
    private float updateIntervalScale = 1f;
    private float ScaledInterval => updateInterval * updateIntervalScale;

    /// <summary>
    /// Scales how often this entity's modules tick. Takes effect after the
    /// current wait completes — UpdateCycle re-reads the field each iteration.
    /// </summary>
    public void SetUpdateIntervalScale(float scale)
    {
        scale = Mathf.Max(1f, scale);
        if (Mathf.Approximately(scale, updateIntervalScale))
            return;

        updateIntervalScale = scale;
        waitForSeconds = new WaitForSeconds(ScaledInterval);
    }
    
    public override void OnNetworkSpawn()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer)
        {
            DisableClientAiComponents();
            enabled = false;
            return;
        }
        
        blackboard?.Initialize();

        foreach (var module in GetComponents<IEntityModule>())
            module?.Initialize();

        healthComponent = blackboard?.healthComponent;

        waitForSeconds = new WaitForSeconds(ScaledInterval);
        updateCoroutine = StartCoroutine(UpdateCycle());

        if (healthComponent != null)
        {
            healthComponent.OnDeath += DisableUpdate;
        }
    }
    
    private IEnumerator UpdateCycle()
    {
        while (true)
        {
            foreach (var t in registeredModules)
            {
                t.PerformUpdate(ScaledInterval);
            }

            yield return waitForSeconds;
        }
    }

    private void DisableClientAiComponents()
    {
        
        if (blackboard == null)
            blackboard = GetComponent<Blackboard>();

        foreach (var module in GetComponentsInChildren<BaseEntityModule>(true))
            SetBehaviourEnabled(module, false);

        if (blackboard == null)
            return;
        
        SetBehaviourEnabled(blackboard.behaviorGraphAgent, false);
        SetBehaviourEnabled(blackboard.fitter, false);
        SetBehaviourEnabled(blackboard.agent, false);
        SetBehaviourEnabled(blackboard.movementModule, false);
        SetBehaviourEnabled(blackboard.rotationModule, false);
        SetBehaviourEnabled(blackboard.attackModule, false);
        SetBehaviourEnabled(blackboard.aggroModule, false);
        SetBehaviourEnabled(blackboard.animatorModule, false);

        
    }

    private static void SetBehaviourEnabled(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null)
            behaviour.enabled = enabled;
    }
    

    public void EnableUpdate()
    {
        if (updateCoroutine != null)
            return;

        if (updateLocks.Count > 0)
            return;

        if (healthComponent && !healthComponent.IsAlive)
            return;

        if (waitForSeconds == null)
            waitForSeconds = new WaitForSeconds(ScaledInterval);

        updateCoroutine = StartCoroutine(UpdateCycle());
    }
    public void DisableUpdate()
    {
        if (updateCoroutine == null)
        {
            return;
        }

        StopCoroutine(updateCoroutine);
        updateCoroutine = null;
    }

    public void AddUpdateLock(object owner)
    {
        if (owner == null)
            return;

        if (updateLocks.Add(owner))
            DisableUpdate();
    }

    public void RemoveUpdateLock(object owner)
    {
        if (owner == null)
            return;

        if (!updateLocks.Remove(owner))
            return;

        if (updateLocks.Count == 0)
            EnableUpdate();
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

        if (healthComponent != null)
        {
            healthComponent.OnDeath -= DisableUpdate;
        }

        registeredModules.Clear();
        updateLocks.Clear();
    }
}
