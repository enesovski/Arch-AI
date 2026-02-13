using Unity.Netcode;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Artifika.AI.Sensors
{
    [Title("Hit Sensor")]
    public class HitSensor : BaseSensor
    {
        [SerializeField, Min(0f)]
        private float referenceDamageValue = 10f;

        private HealthComponent healthComponent;

        public override void Initialize(Transform owner)
        {
            base.Initialize(owner);
            healthComponent = owner.GetComponent<HealthComponent>();
            if (healthComponent)
                healthComponent.OnDamageTakenArgs += OnHitTaken;
        }

        public override void Detect() { }

        private void OnHitTaken(DamageInstance data, float damageAmount)
        {
            GameEntity attacker = ResolveAttackerEntity(data.Source);
            if (!attacker)
                return;

            float rawStrength = Mathf.Clamp01(damageAmount / referenceDamageValue);
            Emit(attacker, rawStrength * strengthMultiplier);
        }

        private GameEntity ResolveAttackerEntity(DamageSource source)
        {
            if (source.SourceObject != null)
            {
                GameEntity direct = source.SourceObject.GetComponent<GameEntity>();
                if (direct) return direct;

                GameEntity parent = source.SourceObject.GetComponentInParent<GameEntity>();
                if (parent) return parent;
            }

            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null)
                return null;

            ulong id = source.OriginatorNetworkObjectId != 0 ? source.OriginatorNetworkObjectId : source.SourceNetworkObjectId;
            if (id == 0)
                return null;

            if (!nm.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject no))
                return null;

            GameEntity entity = no.GetComponent<GameEntity>();
            if (entity) return entity;

            return no.GetComponentInParent<GameEntity>();
        }

        private void OnDisable()
        {
            if (healthComponent != null)
                healthComponent.OnDamageTakenArgs -= OnHitTaken;
        }
    }
}
