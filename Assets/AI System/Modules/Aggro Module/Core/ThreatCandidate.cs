using UnityEngine;

namespace Artifika.AI.Aggro
{
    public readonly struct ThreatCandidate
    {
        public GameEntity Entity { get; }
        public float Score { get; }
        public Vector3 LastKnownPosition { get; }
        public float TimeSinceLastSeen { get; }
        public bool IsVisible { get; }
        public float DistanceToEntity { get; }
        public bool HasHitPressure { get; }

        public ThreatCandidate(
            GameEntity entity,
            float score,
            Vector3 lastKnownPosition,
            float timeSinceLastSeen,
            bool isVisible,
            float distanceToEntity,
            bool hasHitPressure)
        {
            Entity = entity;
            Score = score;
            LastKnownPosition = lastKnownPosition;
            TimeSinceLastSeen = timeSinceLastSeen;
            IsVisible = isVisible;
            DistanceToEntity = distanceToEntity;
            HasHitPressure = hasHitPressure;
        }
    }
}