using UnityEngine;

namespace Artifika.AI
{
    public class ThreatInfo
    {
        public GameEntity Entity { get; private set; }
        public float DetectionStrength { get; set; }
        public float LastDetectionStrength { get; private set; }
        public float HitStrength { get; set; }
        public DamageDeliveryType LastHitDeliveryType { get; private set; }
        public float TotalStrength => DetectionStrength + HitStrength;
        public Vector3 LastKnownPosition { get; set; }
        public float TimeSinceLastSeen { get; set; }

        public ThreatInfo(GameEntity entity)
        {
            Entity = entity;
            LastKnownPosition = entity.transform.position;
        }

        public void UpdatePosition(Vector3 position)
        {
            LastKnownPosition = position;
            TimeSinceLastSeen = 0f;
        }

        public void RecordDetection(float strength, Vector3 position)
        {
            DetectionStrength = strength;
            LastDetectionStrength = strength;
            UpdatePosition(position);
        }

        public void RecordHit(float strength, DamageDeliveryType deliveryType, Vector3 position)
        {
            HitStrength += strength;
            LastHitDeliveryType = deliveryType;
            UpdatePosition(position);
        }

        public void IncrementTimeSinceSeen(float delta)
        {
            TimeSinceLastSeen += delta;
        }
    }
}
