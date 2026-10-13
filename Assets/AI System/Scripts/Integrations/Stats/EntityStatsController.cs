using System;
using Artifika.Stats;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatsComponent))]
public class EntityStatsController : NetworkBehaviour
{
    [Title("Stance")] 
    [SerializeField] private StatusEffectDefinition stunStatusEffectDefinition;

    [Title("Taming")]
    [SerializeField] private bool tamingEnabled;
    [SerializeField, ShowIf(nameof(tamingEnabled))] private GameObject tamingInteractableObject;

    [Title("Debug")]
    [SerializeField, ReadOnly] private bool isStunned;

    private StatsComponent statsComponent;
    private EffectManager effectManager;

    public ResourceStat Stance { get; private set; }
    public bool HasStance => Stance != null;
    public bool IsStunned => isStunned;

    public event Action<bool> OnStunnedChanged;
    public event Action OnStanceDepleted;

    private NetworkVariable<float> stanceSync = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> stanceMaxSync = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isStunnedSync = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> StanceSync => stanceSync;
    public NetworkVariable<float> StanceMaxSync => stanceMaxSync;
    
    private void Awake()
    {
        ResolveReferences();
        ApplyHealthConfigOverrides();
        InitializeStance();
        RefreshTamingInteractable();
    }

    private void ApplyHealthConfigOverrides()
    {
        var healthComponent = GetComponent<HealthComponent>() ??
                              GetComponentInParent<HealthComponent>(true) ??
                              transform.root.GetComponentInChildren<HealthComponent>(true);
        if (healthComponent == null) return;

        var config = healthComponent.GetHealthConfig();
        if (config == null || config.StatOverrides == null || config.StatOverrides.Count == 0) return;

        if (statsComponent == null) return;

        foreach (var statOverride in config.StatOverrides)
        {
            IStat stat = statsComponent.GetStat(statOverride.StatType);
            if (stat == null) continue;

            float delta = statOverride.OverrideValue - stat.BaseValue;
            if (Mathf.Approximately(delta, 0f)) continue;

            var modifier = new StatModifier
            {
                StatType = statOverride.StatType,
                ModifierType = ModifierType.Flat,
                Value = delta,
            };

            stat.AddModifier(modifier);
        }
    }

    public override void OnNetworkSpawn()
    {
        isStunnedSync.OnValueChanged += HandleStunnedSyncChanged;
        ApplyStunnedState(isStunnedSync.Value);

        if (!IsServer) return;

        ResetStance();
        isStunnedSync.Value = isStunned;

        if (Stance != null)
        {
            stanceSync.Value = Stance.CurrentValue;
            stanceMaxSync.Value = Stance.MaxValue;
        }

        SubscribeStanceEvents();
    }

    public override void OnNetworkDespawn()
    {
        isStunnedSync.OnValueChanged -= HandleStunnedSyncChanged;
    }
    
    private void HandleStunnedSyncChanged(bool oldVal, bool newVal)
    {
        if (!IsServer)
            ApplyStunnedState(newVal);
    }
    
    private void InitializeStance()
    {
        if (statsComponent == null) return;

        Stance = statsComponent.GetResourceStat(StatType.Stance);
        if (Stance == null)
        {
            GameLog.Error($"[EntityStatsController] StatsComponent on {name} is missing a Stance ResourceStat.");
            return;
        }
        
        Stance.OnValueChanged += HandleStanceValueChanged;
        Stance.OnMaxValueChanged += HandleStanceMaxValueChanged;
        Stance.InitializeThresholds();
    }

    private void SubscribeStanceEvents()
    {
        if (Stance == null) return;

        Stance.OnValueChanged -= HandleStanceBuildupChanged;
        Stance.OnValueChanged += HandleStanceBuildupChanged;
    }
    
    public void ApplyStanceDamage(float amount)
    {
        if (!IsServer || Stance == null || amount <= 0f || isStunned) return;
        Stance.Regenerate(amount);
    }

    public void RegenerateStance(float amount)
    {
        if (!IsServer || Stance == null || amount <= 0f || isStunned) return;
        Stance.Drain(amount);
    }

    public void ResetStance()
    {
        if (!IsServer || Stance == null) return;
        Stance.SetCurrent(0f);
    }

    public void FillStance()
    {
        if (!IsServer || Stance == null) return;
        Stance.SetCurrent(Stance.MaxValue);
    }
    
    private void HandleStanceBuildupChanged(float value)
    {
        if (isStunned || Stance == null || value < Stance.MaxValue) return;
        OnStanceDepleted?.Invoke();
        EnterStun();
    }

    private void HandleStanceValueChanged(float value)
    {

        if (IsServer)
            stanceSync.Value = value;
    }

    private void HandleStanceMaxValueChanged(float value)
    {
        if (IsServer)
            stanceMaxSync.Value = value;
    }
    
    private void EnterStun()
    {
        if (isStunned) return;

        SetStunned(true);

        if (stunStatusEffectDefinition == null) return;
        if (effectManager == null)
            ResolveReferences();

        if (effectManager == null)
        {
            GameLog.Error($"[EntityStatsController] EffectManager is missing on {name}; cannot apply stun effect.");
            return;
        }

        effectManager.AddEffect(stunStatusEffectDefinition);
    }

    public void NotifyStunEffectRemoved()
    {
        if (!IsServer || !isStunned) return;
        SetStunned(false);
        ResetStance();
    }

    public void NotifyStunEffectApplied()
    {
        if (!IsServer || isStunned) return;
        SetStunned(true);
    }

    public bool TryGetStunTiming(out float remainingSeconds, out float durationSeconds)
    {
        remainingSeconds = 0f;
        durationSeconds = 0f;

        if (!isStunned || stunStatusEffectDefinition == null)
            return false;

        if (effectManager == null)
            ResolveReferences();

        IEffectDefinition stunDefinition = stunStatusEffectDefinition;
        durationSeconds = Mathf.Max(0f, stunDefinition.DurationSeconds);
        return durationSeconds > 0f &&
               effectManager != null &&
               effectManager.TryGetRemainingTime(stunDefinition, out remainingSeconds);
    }

    public bool TryReduceStunTime(
        float requestedSeconds,
        out float remainingSeconds,
        out float durationSeconds,
        out float removedSeconds)
    {
        remainingSeconds = 0f;
        durationSeconds = 0f;
        removedSeconds = 0f;

        if (!IsServer || !isStunned || requestedSeconds <= 0f ||
            stunStatusEffectDefinition == null)
        {
            return false;
        }

        if (effectManager == null)
            ResolveReferences();

        IEffectDefinition stunDefinition = stunStatusEffectDefinition;
        durationSeconds = Mathf.Max(0f, stunDefinition.DurationSeconds);
        return effectManager != null &&
               effectManager.TryReduceRemainingTime(
                   stunDefinition,
                   requestedSeconds,
                   out remainingSeconds,
                   out removedSeconds);
    }

    private void SetStunned(bool stunned)
    {
        if (isStunned == stunned) return;

        ApplyStunnedState(stunned);

        if (IsServer)
            isStunnedSync.Value = stunned;
    }

    private void ApplyStunnedState(bool stunned)
    {
        if (isStunned == stunned)
        {
            RefreshTamingInteractable();
            return;
        }

        isStunned = stunned;
        RefreshTamingInteractable();
        OnStunnedChanged?.Invoke(isStunned);
    }

    private void RefreshTamingInteractable()
    {
        if (tamingInteractableObject != null)
            tamingInteractableObject.SetActive(tamingEnabled && isStunned);
    }

    private void ResolveReferences()
    {
        if (statsComponent == null)
        {
            statsComponent = GetComponent<StatsComponent>() ??
                             GetComponentInParent<StatsComponent>(true) ??
                             transform.root.GetComponentInChildren<StatsComponent>(true);
        }

        if (effectManager == null)
        {
            effectManager = GetComponent<EffectManager>() ??
                            GetComponentInParent<EffectManager>(true) ??
                            transform.root.GetComponentInChildren<EffectManager>(true);
        }
    }


    public override void OnDestroy()
    {
        if (Stance != null)
        {
            Stance.OnValueChanged -= HandleStanceValueChanged;
            Stance.OnMaxValueChanged -= HandleStanceMaxValueChanged;
            Stance.OnValueChanged -= HandleStanceBuildupChanged;
        }

        base.OnDestroy();
    }
}
