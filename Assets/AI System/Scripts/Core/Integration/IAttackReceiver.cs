using Archura.Attack;
using Unity.Netcode;
using UnityEngine;

namespace Archura
{
    /// <summary>
    /// A melee hit the AI has decided to land. The AI owns detection, targeting and
    /// faction checks; the host owns turning this into its own damage model.
    /// </summary>
    public readonly struct MeleeHit
    {
        public readonly GameEntity Attacker;
        public readonly NetworkObject AttackerNetworkObject;

        /// <summary>The attack that produced the hit. Its <c>DamageConfig</c> carries the host's damage data.</summary>
        public readonly MeleeAttackDefinition Definition;

        public readonly Collider HitCollider;
        public readonly Vector3 ImpactPoint;
        public readonly Vector3 Direction;

        /// <summary>Initial knockback velocity, or zero when the attack has none.</summary>
        public readonly Vector3 KnockbackImpulse;

        /// <summary>Horizontal knockback deceleration (m/s²).</summary>
        public readonly float KnockbackDeceleration;

        public MeleeHit(GameEntity attacker, NetworkObject attackerNetworkObject, MeleeAttackDefinition definition,
            Collider hitCollider, Vector3 impactPoint, Vector3 direction,
            Vector3 knockbackImpulse, float knockbackDeceleration)
        {
            Attacker = attacker;
            AttackerNetworkObject = attackerNetworkObject;
            Definition = definition;
            HitCollider = hitCollider;
            ImpactPoint = impactPoint;
            Direction = direction;
            KnockbackImpulse = knockbackImpulse;
            KnockbackDeceleration = knockbackDeceleration;
        }
    }

    /// <summary>
    /// Receives AI melee hits on the server. Implement it on the health component, or
    /// on individual hitbox colliders that need their own handling. Knockback should
    /// only be applied when the hit actually dealt damage.
    /// </summary>
    public interface IAttackReceiver
    {
        void ReceiveMeleeHit(in MeleeHit hit);
    }

    /// <summary>Optional per-collider hit presentation (sparks, decals, sounds).</summary>
    public interface IHitSurface
    {
        void PlayHitEffect(Vector3 impactPoint, Vector3 direction);
    }
}
