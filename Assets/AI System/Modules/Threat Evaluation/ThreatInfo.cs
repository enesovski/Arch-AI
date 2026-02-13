using UnityEngine;

namespace Artifika.AI
{
    public class ThreatInfo
    {
        public GameEntity Entity { get; private set; }
        public float DetectionStrength { get; set; }
        public float HitStrength { get; set; }
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

        public void IncrementTimeSinceSeen(float delta)
        {
            TimeSinceLastSeen += delta;
        }
    }
}