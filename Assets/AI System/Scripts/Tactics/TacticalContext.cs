namespace Archura.Utility
{
    public struct TacticalContext
    {
        public bool HasTarget;
        public float Distance;

        public float OwnHealthNormalized;
        public float TargetHealthNormalized;

        // Raw DetectionModule score; axis assets own their normalization ranges.
        public float ThreatScore;

        public bool CanStartAttack;
        public bool HasLineOfSight;
        public float TimeSinceLastAttack;

        public int TargetAggroCount;

        public float Aggressiveness;
    }
}
