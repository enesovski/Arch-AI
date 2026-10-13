using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Artifika.AI.Attack;
using UnityEngine;

public class MeleeAttackModule : AttackSubModule
{
    [Title("Attack Detection")]
    [SerializeField] private LayerMask attackableLayers;

    private readonly HashSet<HealthComponent> hitList = new HashSet<HealthComponent>();
    public int AttackableLayerMask => attackableLayers.value;

    public override void Attack(BaseAttackDefinition def)
    {
        AttackModule.AttackData attackData = attackModule.FindAttackData(def);
        PerformAttack(def as MeleeAttackDefinition, attackData.attackPoint, attackData.attackPerformFeedback);
    }

    private void PerformAttack(MeleeAttackDefinition meleeDef, Transform attackPoint, MMF_Player feedbackPlayer)
    {
        if (attackPoint == null)
        {
            InvokeOnAttack();
            feedbackPlayer?.PlayFeedbacks();
            return;
        }

        hitList.Clear();

        Vector3 pointA = attackPoint.position;
        Vector3 pointB = attackPoint.position + transform.forward * meleeDef.AttackRange;

        Collider[] hits = Physics.OverlapCapsule(pointA, pointB, meleeDef.AttackRadius, 
            attackableLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in hits)
        {
            HealthComponent coreHealth = col.GetComponentInParent<HealthComponent>() ?? col.GetComponentInChildren<HealthComponent>();

            if (coreHealth == null ||
                coreHealth.NetworkObject == selfNetworkObject ||
                !hitList.Add(coreHealth))
                continue;

            GameEntity targetEntity = coreHealth.GetComponent<GameEntity>();
            if (targetEntity != null &&
                !FactionRelationsSO.AllowsDamage(gameEntity, targetEntity))
                continue;

            IDamageable damageable = col.GetComponent<IDamageable>() ?? (IDamageable)coreHealth;
                
            Vector3 impactPoint = col.ClosestPoint(transform.position);
            Vector3 dir = (col.transform.position - transform.position).normalized;

            DamagePayload payload = DamagePayload.Builder()
                .Add(DamageType.Physical, meleeDef.PhysicalDamage)
                .Add(DamageType.Elysiorite, meleeDef.ElysioriteDamage)
                .Build();

            DamageContext context = new DamageContext(
                impactPoint: impactPoint,
                direction: dir,
                deliveryType: DamageDeliveryType.Melee
            );

            DamageSource source = DamageSourceFactory.FromEnemy(gameEntity, selfNetworkObject);

            DamageModifiers modifiers = DamageModifiers.Builder().Build();

            DamageInstance instance = new DamageInstance(
                source: source,
                payload: payload,
                context: context,
                modifiers: modifiers,
                stanceDamage: meleeDef.StunDamage
            );

            float healthBeforeHit = coreHealth.Health;
            damageable.TakeDamage(instance);
            if (meleeDef.KnockbackEnabled && coreHealth.Health < healthBeforeHit &&
                coreHealth is Artifika.Player.PlayerHealthComponent playerHealth)
            {
                Vector3 away = playerHealth.transform.position - transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                Vector3 impulse = away.normalized * meleeDef.KnockbackHorizontalSpeed +
                                  Vector3.up * meleeDef.KnockbackUpwardSpeed;
                playerHealth.ApplyKnockbackServer(impulse, meleeDef.KnockbackDeceleration);
            }
            InvokeOnAttack();

            IHittableSurface hittableSurface = col.GetComponent<IHittableSurface>();
            hittableSurface?.PlayHitEffect(impactPoint, dir);
        }

        feedbackPlayer?.PlayFeedbacks();
    }

}
