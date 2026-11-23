using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace Archura.Sensors
{
    [Title("Hit Sensor")]
    public class HitSensor : BaseSensor
    {
        public event System.Action<GameEntity, float, DamageDeliveryType> OnHitDetected;

        [SerializeField, Min(0f)]
        private float referenceDamageValue = 10f;

        private IEntityHealth healthComponent;
        private bool healthSubscribed;

        public override void Initialize(GameEntity owner)
        {
            base.Initialize(owner);
            UnsubscribeFromHealth();
            healthComponent = owner.GetComponent<IEntityHealth>();
            SubscribeToHealth();
        }

        private void OnEnable() => SubscribeToHealth();

        private void SubscribeToHealth()
        {
            if (!isActiveAndEnabled || UnityNull.IsNull(healthComponent) || healthSubscribed)
                return;

            healthComponent.OnDamageReceived += OnHitTaken;
            healthSubscribed = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (!healthSubscribed)
                return;

            if (!UnityNull.IsNull(healthComponent))
                healthComponent.OnDamageReceived -= OnHitTaken;
            healthSubscribed = false;
        }

        public override void Detect() { }

        private void OnHitTaken(DamageReport report)
        {
            GameEntity attacker = ResolveAttackerEntity(report);
            if (!attacker)
                return;

            float rawStrength = Mathf.Clamp01(report.Amount / referenceDamageValue);
            OnHitDetected?.Invoke(attacker, rawStrength * strengthMultiplier, report.DeliveryType);
        }

        private GameEntity ResolveAttackerEntity(DamageReport source)
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
