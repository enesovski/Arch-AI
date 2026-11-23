using System.Collections.Generic;
using Archura.Attack;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public class MeleeAttackModule : AttackSubModule
    {
        [Title("Attack Detection")]
        [SerializeField] private LayerMask attackableLayers;

        private readonly HashSet<IEntityHealth> hitList = new HashSet<IEntityHealth>();
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
                IEntityHealth coreHealth = col.GetComponentInParent<IEntityHealth>() ?? col.GetComponentInChildren<IEntityHealth>();
                Component healthOwner = coreHealth as Component;

                if (healthOwner == null ||
                    healthOwner.GetComponentInParent<Unity.Netcode.NetworkObject>() == selfNetworkObject ||
                    !hitList.Add(coreHealth))
                    continue;

                GameEntity targetEntity = healthOwner.GetComponent<GameEntity>();
                if (targetEntity != null &&
                    !FactionRelationsSO.AllowsDamage(gameEntity, targetEntity))
                    continue;

                // A hitbox may handle its own hits (weak points); otherwise the health owner does.
                IAttackReceiver receiver = col.GetComponent<IAttackReceiver>() ?? coreHealth as IAttackReceiver;

                Vector3 impactPoint = col.ClosestPoint(transform.position);
                Vector3 dir = (col.transform.position - transform.position).normalized;

                Vector3 impulse = Vector3.zero;
                if (meleeDef.KnockbackEnabled)
                {
                    Vector3 away = healthOwner.transform.position - transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.0001f)
                        away = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                    impulse = away.normalized * meleeDef.KnockbackHorizontalSpeed +
                              Vector3.up * meleeDef.KnockbackUpwardSpeed;
                }

                receiver?.ReceiveMeleeHit(new MeleeHit(
                    attacker: gameEntity,
                    attackerNetworkObject: selfNetworkObject,
                    definition: meleeDef,
                    hitCollider: col,
                    impactPoint: impactPoint,
                    direction: dir,
                    knockbackImpulse: impulse,
                    knockbackDeceleration: meleeDef.KnockbackDeceleration));
                InvokeOnAttack();

                IHitSurface hitSurface = col.GetComponent<IHitSurface>();
                hitSurface?.PlayHitEffect(impactPoint, dir);
            }

            feedbackPlayer?.PlayFeedbacks();
        }

    }
}
