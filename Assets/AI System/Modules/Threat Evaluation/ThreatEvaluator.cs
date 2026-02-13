using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artifika.AI
{
    public class ThreatEvaluator
    {
        private readonly Dictionary<GameEntity, ThreatInfo> threats;
        private readonly ThreatEvaluationSettings settings;
        private readonly List<GameEntity> removalBuffer = new List<GameEntity>();

        public ThreatEvaluator(ThreatEvaluationSettings settings)
        {
            this.settings = settings;
            threats = new Dictionary<GameEntity, ThreatInfo>();
        }


        public void GetThreatEntities(List<GameEntity> results)
        {
            results.Clear();
            removalBuffer.Clear();

            foreach (var entity in threats.Select(kvp => kvp.Key))
            {
                if (!entity || !IsEntityValid(entity))
                {
                    removalBuffer.Add(entity);
                    continue;
                }

                results.Add(entity);
            }

            for (int i = 0; i < removalBuffer.Count; i++)
                threats.Remove(removalBuffer[i]);
        }

        public ThreatInfo GetThreat(GameEntity entity)
        {
            return threats.TryGetValue(entity, out var threat) ? threat : null;
        }

        public bool HasThreat(GameEntity entity)
        {
            return threats.ContainsKey(entity);
        }


        public void UpdateThreat(GameEntity entity, float detectionStrength)
        {
            if (!threats.TryGetValue(entity, out ThreatInfo threat))
            {
                threat = new ThreatInfo(entity);
                threats.Add(entity, threat);
            }

            threat.DetectionStrength = detectionStrength;
            threat.UpdatePosition(entity.transform.position);
        }

        public void RecordHit(GameEntity attacker, float hitStrength)
        {
            if (!threats.TryGetValue(attacker, out ThreatInfo threat))
            {
                threat = new ThreatInfo(attacker);
                threats.Add(attacker, threat);
            }

            threat.HitStrength += hitStrength;
            threat.UpdatePosition(attacker.transform.position);
        }

        public void ClearNonHitThreats()
        {
            foreach (var threat in threats.Values)
                threat.DetectionStrength = 0f;
        }

        public void DecayThreats(float deltaTime)
        {
            removalBuffer.Clear();

            foreach (var kvp in threats)
            {
                ThreatInfo threat = kvp.Value;

                if (threat.HitStrength > 0f)
                {
                    threat.HitStrength -= settings.hitDecayRate * deltaTime;
                    if (threat.HitStrength < 0f)
                        threat.HitStrength = 0f;
                }

                threat.IncrementTimeSinceSeen(deltaTime);

                if (threat.TotalStrength <= 0f)
                    removalBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < removalBuffer.Count; i++)
                threats.Remove(removalBuffer[i]);
        }


        public ThreatEvaluation Evaluate()
        {
            ThreatInfo highestThreat = null;
            float highestScore = float.NegativeInfinity;

            foreach (var kvp in threats)
            {
                ThreatInfo threat = kvp.Value;

                if (!IsEntityValid(threat.Entity))
                    continue;

                float score = CalculateThreatScore(threat);

                if (score > highestScore)
                {
                    highestScore = score;
                    highestThreat = threat;
                }
            }

            if (highestScore >= settings.aggroThreshold)
            {
                return new ThreatEvaluation(
                    ThreatLevel.Alerted,
                    highestThreat?.Entity,
                    highestThreat?.LastKnownPosition ?? Vector3.zero,
                    highestScore);
            }

            if (highestScore >= settings.suspiciousThreshold)
            {
                return new ThreatEvaluation(
                    ThreatLevel.Suspicious,
                    highestThreat?.Entity,
                    highestThreat?.LastKnownPosition ?? Vector3.zero,
                    highestScore);
            }

            return new ThreatEvaluation(ThreatLevel.None, null, Vector3.zero, 0f);
        }

        public float CalculateThreatScore(ThreatInfo threat)
        {
            float score = threat.TotalStrength;

            if (threat.TimeSinceLastSeen > settings.memoryDecayStartTime)
            {
                float elapsed = threat.TimeSinceLastSeen - settings.memoryDecayStartTime;
                float decayFactor = 1f - (elapsed / settings.memoryDecayDuration);
                score *= Mathf.Clamp01(decayFactor);
            }

            return score;
        }

        private bool IsEntityValid(GameEntity entity)
        {
            if (!entity)
                return false;

            HealthComponent health = entity.healthComponent;
            
            return !health || health.IsAlive;
        }

        public void Clear()
        {
            threats.Clear();
        }
    }
}