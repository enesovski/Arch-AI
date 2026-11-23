using UnityEngine;

namespace Archura
{
    /// <summary>How a hit reached its target. Perception uses it to weigh threats.</summary>
    public enum DamageDeliveryType
    {
        Direct,
        Melee,
        Ranged,
        Projectile,
        Environment,
        StatusEffect
    }

    /// <summary>
    /// The part of a damage event the AI cares about. The host translates its own
    /// damage data into this when raising <see cref="IEntityHealth.OnDamageReceived"/>.
    /// </summary>
    public readonly struct DamageReport
    {
        public readonly float Amount;
        public readonly DamageDeliveryType DeliveryType;
        public readonly Vector3 ImpactPoint;

        /// <summary>Object that dealt the damage (weapon, projectile, attacker). May be null.</summary>
        public readonly GameObject SourceObject;

        /// <summary>Network id of <see cref="SourceObject"/>, used when the object is gone. 0 if unknown.</summary>
        public readonly ulong SourceNetworkObjectId;

        /// <summary>Network id of the entity that started the attack (e.g. a projectile's shooter). 0 if unknown.</summary>
        public readonly ulong OriginatorNetworkObjectId;

        public DamageReport(float amount, DamageDeliveryType deliveryType, Vector3 impactPoint,
            GameObject sourceObject = null, ulong sourceNetworkObjectId = 0, ulong originatorNetworkObjectId = 0)
        {
            Amount = amount;
            DeliveryType = deliveryType;
            ImpactPoint = impactPoint;
            SourceObject = sourceObject;
            SourceNetworkObjectId = sourceNetworkObjectId;
            OriginatorNetworkObjectId = originatorNetworkObjectId;
        }
    }
}
