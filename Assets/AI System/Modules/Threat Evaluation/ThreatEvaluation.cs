using UnityEngine;

namespace Artifika.AI
{
    public enum ThreatLevel
    {
        None,
        Suspicious,
        Alerted
    }
    public struct ThreatEvaluation
    {
        public ThreatLevel Level { get; }
        public GameEntity Entity { get; }
        public Vector3 LastKnownPosition { get; }
        public float ThreatScore { get; }

        public ThreatEvaluation(ThreatLevel level, GameEntity entity, Vector3 lastKnownPosition, float threatScore)
        {
            Level = level;
            Entity = entity;
            LastKnownPosition = lastKnownPosition;
            ThreatScore = threatScore;
        }
    }
}

