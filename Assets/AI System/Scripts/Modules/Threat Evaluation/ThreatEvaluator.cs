using System.Collections.Generic;
using Artifika.AI.Aggro;
using UnityEngine;

namespace Artifika.AI
{
    public class ThreatEvaluator
    {
        private readonly Dictionary<GameEntity, ThreatInfo> threats;
        private readonly AggroConfiguration _configuration;
        private readonly List<GameEntity> removalBuffer = new List<GameEntity>();

        public ThreatEvaluator(AggroConfiguration configuration)
        {
            this._configuration = configuration;
            threats = new Dictionary<GameEntity, ThreatInfo>();
        }

        public void GetThreatEntities(List<GameEntity> results)
        {
            results.Clear();
            removalBuffer.Clear();

            foreach (GameEntity entity in threats.Keys)
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

            threat.RecordDetection(detectionStrength, entity.transform.position);
        }

        public void RecordHit(GameEntity attacker, float hitStrength, DamageDeliveryType deliveryType)
        {
            if (!threats.TryGetValue(attacker, out ThreatInfo threat))
            {
                threat = new ThreatInfo(attacker);
                threats.Add(attacker, threat);
            }

            threat.RecordHit(hitStrength, deliveryType, attacker.transform.position);
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
                    threat.HitStrength -= _configuration.hitDecayRate * deltaTime;
                    if (threat.HitStrength < 0f)
                        threat.HitStrength = 0f;
                }

                threat.IncrementTimeSinceSeen(deltaTime);

                if (CalculateThreatScore(threat) <= 0f)
                    removalBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < removalBuffer.Count; i++)
                threats.Remove(removalBuffer[i]);
        }

        public float CalculateThreatScore(ThreatInfo threat)
        {
            float detectionScore = threat.DetectionStrength > 0f
                ? threat.DetectionStrength
                : threat.LastDetectionStrength;

            if (threat.TimeSinceLastSeen > _configuration.memoryDecayStartTime)
            {
                float elapsed = threat.TimeSinceLastSeen - _configuration.memoryDecayStartTime;
                float decayFactor = 1f - (elapsed / _configuration.memoryDecayDuration);
                detectionScore *= Mathf.Clamp01(decayFactor);
            }

            return detectionScore + threat.HitStrength * _configuration.hitMultiplier;
        }

        private bool IsEntityValid(GameEntity entity)
        {
            if (!entity)
                return false;

            HealthComponent health = entity.HealthComponent;
            return !health || health.IsAlive;
        }

        public void Clear()
        {
            threats.Clear();
        }
    }
}
