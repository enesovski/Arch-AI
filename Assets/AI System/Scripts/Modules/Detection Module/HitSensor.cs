using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace Artifika.AI.Sensors
{
    [Title("Hit Sensor")]
    public class HitSensor : BaseSensor
    {
        public event System.Action<GameEntity, float, DamageDeliveryType> OnHitDetected;

        [SerializeField, Min(0f)]
        private float referenceDamageValue = 10f;

        private HealthComponent healthComponent;
        private bool healthSubscribed;

        public override void Initialize(GameEntity owner)
        {
            base.Initialize(owner);
            UnsubscribeFromHealth();
            healthComponent = owner.GetComponent<HealthComponent>();
            SubscribeToHealth();
        }

        private void OnEnable() => SubscribeToHealth();

        private void SubscribeToHealth()
        {
            if (!isActiveAndEnabled || !healthComponent || healthSubscribed)
                return;

            healthComponent.OnDamageTakenArgs += OnHitTaken;
            healthSubscribed = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (!healthSubscribed)
                return;

            if (healthComponent)
                healthComponent.OnDamageTakenArgs -= OnHitTaken;
            healthSubscribed = false;
        }

        public override void Detect() { }

        private void OnHitTaken(DamageInstance data, float damageAmount)
        {
            GameEntity attacker = ResolveAttackerEntity(data.Source);
            if (!attacker)
                return;

            float rawStrength = Mathf.Clamp01(damageAmount / referenceDamageValue);
            OnHitDetected?.Invoke(attacker, rawStrength * strengthMultiplier, data.Context.DeliveryType);
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

        private void OnDisable() => UnsubscribeFromHealth();
    }
}
