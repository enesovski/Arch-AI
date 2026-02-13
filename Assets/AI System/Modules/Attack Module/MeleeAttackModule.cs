using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Artifika.AI.Attack;
using UnityEngine;

public class MeleeAttackModule : AttackSubModule
{
    [Title("Attack Detection")]
    [SerializeField] LayerMask attackableLayers;

    private HashSet<HealthComponent> hitList = new HashSet<HealthComponent>();

    public override void Attack(BaseAttackDefinition def)
    {
        AttackModule.AttackData attackData = attackModule.FindAttackData(def);
        PerformAttack(def as MeleeAttackDefinition, attackData.attackPoint, attackData.attackPerformFeedback);
    }

    private void PerformAttack(MeleeAttackDefinition meleeDef, Transform attackPoint, MMF_Player feedbackPlayer)
    {
        if(attackPoint == null)
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
            HealthComponent healthComponent = col.GetComponentInParent<HealthComponent>();
            if (healthComponent == null)
                healthComponent = col.GetComponentInChildren<HealthComponent>();

            if (healthComponent != null && hitList.Add(healthComponent))
            {
                Vector3 impactPoint = col.ClosestPoint(transform.position);
                Vector3 dir = (col.transform.position - transform.position).normalized;

                DamagePayloadBuilder payloadBuilder = DamagePayload.Builder()
                    .Add(DamageType.Physical, meleeDef.PhysicalDamage)
                    .Add(DamageType.Piercing, meleeDef.PiercingDamage)
                    .Add(DamageType.Elysiorite, meleeDef.ElysioriteDamage);

                DamagePayload payload = payloadBuilder.Build();

                DamageContext context = new DamageContext(
                    impactPoint: impactPoint,
                    direction: dir
                );

                DamageSource source = DamageSourceFactory.FromEnemy(gameEntity, selfNetworkObject);

                DamageModifiers modifiers = DamageModifiers.Builder().Build();

                DamageInstance instance = new DamageInstance(
                    source: source,
                    payload: payload,
                    context: context,
                    modifiers: modifiers
                );

                ((IDamageable)healthComponent).TakeDamage(instance);
                InvokeOnAttack();

                IHittableSurface hittableSurface = col.GetComponent<IHittableSurface>();
                hittableSurface?.PlayHitEffect(impactPoint, dir);
            }

        }

        feedbackPlayer?.PlayFeedbacks();
    }

    /*private void OnDisable()
    {
        eventHandler.onAttackAnimation -= Attack;
    }*/

}
