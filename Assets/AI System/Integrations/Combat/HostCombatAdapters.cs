// Reference host adapters (not compiled by this project: the "~" folder is ignored
// by Unity). Copy into the host game to connect its combat systems to Archura.
// Each adapter is the only place that knows both sides of a contract.

using System;
using Archura;
using Archura.Attack;
using HighlightPlus;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Exposes the game's <see cref="HealthComponent"/> to the AI and turns AI melee hits
/// into game damage. Add it next to every HealthComponent the AI should perceive or hit
/// (enemies, companions and players).
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public sealed class HealthComponentAIBridge : MonoBehaviour, IEntityHealth, IAttackReceiver
{
    private HealthComponent health;

    public float Health => health.Health;
    public float MaxHealth => health.MaxHealth;
    public bool IsAlive => health.IsAlive;

    public event Action OnDeath;
    public event Action OnServerDeath;
    public event Action<DamageReport> OnDamageReceived;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        health.OnDeath += RaiseDeath;
        health.OnServerDeathArgs += RaiseServerDeath;
        health.OnDamageTakenArgs += RaiseDamage;
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.OnDeath -= RaiseDeath;
        health.OnServerDeathArgs -= RaiseServerDeath;
        health.OnDamageTakenArgs -= RaiseDamage;
    }

    private void RaiseDeath() => OnDeath?.Invoke();
    private void RaiseServerDeath(DamageInstance killingBlow) => OnServerDeath?.Invoke();

    private void RaiseDamage(DamageInstance data, float amount)
    {
        OnDamageReceived?.Invoke(new DamageReport(
            amount,
            (Archura.DamageDeliveryType)(int)data.Context.DeliveryType,
            data.Context.ImpactPoint,
            data.Source.SourceObject,
            data.Source.SourceNetworkObjectId,
            data.Source.OriginatorNetworkObjectId));
    }

    public void ReceiveMeleeHit(in MeleeHit hit)
    {
        var damageConfig = hit.Definition.DamageConfig as DamageConfig;

        DamagePayload payload = DamagePayload.Builder()
            .Add(DamageType.Physical, damageConfig != null ? damageConfig.physicalDamage : 0f)
            .Add(DamageType.Elysiorite, damageConfig != null ? damageConfig.elysioriteDamage : 0f)
            .Build();

        var instance = new DamageInstance(
            source: DamageSourceFactory.FromEnemy(hit.Attacker, hit.AttackerNetworkObject),
            payload: payload,
            context: new DamageContext(
                impactPoint: hit.ImpactPoint,
                direction: hit.Direction,
                deliveryType: global::DamageDeliveryType.Melee),
            modifiers: DamageModifiers.Builder().Build(),
            stanceDamage: damageConfig != null ? damageConfig.stunDamage : 0f);

        // Hitbox colliders (weak points) keep their own IDamageable handling.
        IDamageable damageable = hit.HitCollider.GetComponent<IDamageable>() ?? health;

        float healthBeforeHit = health.Health;
        damageable.TakeDamage(instance);

        if (hit.KnockbackImpulse != Vector3.zero && health.Health < healthBeforeHit &&
            health is Artifika.Player.PlayerHealthComponent playerHealth)
        {
            playerHealth.ApplyKnockbackServer(hit.KnockbackImpulse, hit.KnockbackDeceleration);
        }

        hit.HitCollider.GetComponent<IHittableSurface>()?.PlayHitEffect(hit.ImpactPoint, hit.Direction);
    }
}

/// <summary>Plays HighlightPlus' hit flash for <c>VisualEffectsModule</c>.</summary>
[RequireComponent(typeof(HighlightEffect))]
public sealed class HighlightHitAdapter : MonoBehaviour, IHitHighlight
{
    private HighlightEffect effect;

    private void Awake() => effect = GetComponent<HighlightEffect>();

    public void PlayHitHighlight(Vector3 impactPoint) => effect.HitFX();
}

// Small additions to existing game classes (shown as the members to add):
//
// public class AnimatorEventHandler : MonoBehaviour, Archura.IAttackAnimationEventSource
// {
//     event Action<BaseAttackDefinition> IAttackAnimationEventSource.AttackAnimationTriggered
//     {
//         add => OnAttackAnimation += value.Invoke;
//         remove => OnAttackAnimation -= value.Invoke;
//     }
//     void IAttackAnimationEventSource.TriggerAttackAnimation(BaseAttackDefinition def) => onAttackAnimationInvoke(def);
// }
//
// public class ProjectileBase : NetworkBehaviour, Archura.IProjectile
// {
//     void IProjectile.Initialize(Vector3 target, NetworkObject owner) => Initialize(target, owner);
// }
//
// public class AIGroundFitter : FSimpleFitter, Archura.IGroundFitter
// {
//     bool IGroundFitter.UseExternalYaw { get => UseExternalYaw; set => UseExternalYaw = value; }
//     float IGroundFitter.ExternalYaw { get => ExternalYaw; set => ExternalYaw = value; }
//     float IGroundFitter.UpAxisRotation { get => UpAxisRotation; set => UpAxisRotation = value; }
//     bool IGroundFitter.SuppressRootMotionTranslation { get => SuppressRootMotionTranslation; set => SuppressRootMotionTranslation = value; }
//     bool IGroundFitter.HandleRootMotion { get => HandleRootMotion; set => HandleRootMotion = value; }
//     LayerMask IGroundFitter.GroundLayerMask => GroundLayerMask;
// }
//
// AnimatorEffectPlayer : Archura.IAnimationEffectPlayer   (already has PlayEffect(string))
// HarvestAnimRelay     : Archura.IAnimationEventReceiver  (call AE_HarvestInstant() for "AE_HarvestInstant")
// EntityStatsController (Integrations~/Stats) already implements Archura.IStunState.
