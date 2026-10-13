using UnityEngine;

namespace Artifika.AI.Aggro
{
    public readonly struct EngagementContext
    {
        public EngagementState State { get; }
        public GameEntity Target { get; }
        public float TargetScore { get; }
        public Vector3 LastKnownPosition { get; }
        public float TimeSinceLastSeen { get; }
        public bool IsTargetVisible { get; }
        public float DistanceToTarget { get; }

        public bool HasTarget => Target != null;

        public EngagementContext(
            EngagementState state,
            GameEntity target,
            float targetScore,
            Vector3 lastKnownPosition,
            float timeSinceLastSeen,
            bool isTargetVisible,
            float distanceToTarget)
        {
            State = state;
            Target = target;
            TargetScore = targetScore;
            LastKnownPosition = lastKnownPosition;
            TimeSinceLastSeen = timeSinceLastSeen;
            IsTargetVisible = isTargetVisible;
            DistanceToTarget = distanceToTarget;
        }
    }
}