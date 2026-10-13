using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Artifika.AI.Aggro;
using Artifika.AI.Sensors;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artifika.AI
{
    public class DetectionModule : UpdatableEntityModule
    {
        public const int MAX_TARGET_COUNT = 16;

        private bool IsEnemy(GameEntity entity)
        {
            return FactionRelationsSO.AllowsDamage(blackboard.gameEntity, entity);
        }

        [Title("Config")]
        [SerializeField, Required, AssetsOnly] private AggroConfiguration configuration;

        public event Action<GameEntity, float> OnDetectionUpdate;
        public event Action<GameEntity, float> OnHitDetected;

        private float detectionTimeInterval;
        private List<BaseSensor> sensorComponents;
        private ThreatEvaluator threatEvaluator;
        private bool sensorsSubscribed;
        private LayerMask losMask;

        private readonly Dictionary<GameEntity, float> currentCycleDetections = new Dictionary<GameEntity, float>();

        protected override void OnInitialize()
        {
            threatEvaluator = new ThreatEvaluator(configuration);
            sensorComponents = new List<BaseSensor>(GetComponentsInChildren<BaseSensor>(true));

            detectionTimeInterval = GetComponent<EntityCoordinator>().GetUpdateInterval();

            foreach (var sensor in sensorComponents)
            {
                sensor.Initialize(blackboard.gameEntity);

                if (sensor is VisualSensor visualSensor)
                    losMask = visualSensor.ObstacleMask;
            }
        }

        public override void OnModuleEnabled()
        {
            if (sensorsSubscribed || sensorComponents == null)
                return;

            foreach (var sensor in sensorComponents)
            {
                if (!sensor) continue;

                if (sensor is HitSensor hitSensor)
                    hitSensor.OnHitDetected += HandleHitDetection;
                else
                    sensor.OnDetected += HandleSensorDetection;
            }

            sensorsSubscribed = true;
        }

        public override void OnModuleDisabled()
        {
            if (!sensorsSubscribed || sensorComponents == null)
                return;

            foreach (var sensor in sensorComponents)
            {
                if (!sensor) continue;

                if (sensor is HitSensor hitSensor)
                    hitSensor.OnHitDetected -= HandleHitDetection;
                else
                    sensor.OnDetected -= HandleSensorDetection;
            }

            sensorsSubscribed = false;
        }

        public override void PerformUpdate(float deltaTime)
        {
            currentCycleDetections.Clear();

            foreach (var sensor in sensorComponents)
            {
                if (!sensor || !sensor.isActiveAndEnabled || sensor is HitSensor)
                    continue;
                sensor.Detect();
            }

            threatEvaluator.ClearNonHitThreats();

            foreach (var kvp in currentCycleDetections)
            {
                threatEvaluator.UpdateThreat(kvp.Key, kvp.Value);
                OnDetectionUpdate?.Invoke(kvp.Key, kvp.Value);
            }

            threatEvaluator.DecayThreats(detectionTimeInterval);
        }

        public void GetThreatEntities(List<GameEntity> results)
        {
            threatEvaluator.GetThreatEntities(results);
        }

        public float GetThreatScore(GameEntity entity)
        {
            ThreatInfo threat = threatEvaluator.GetThreat(entity);
            return threat == null ? 0f : threatEvaluator.CalculateThreatScore(threat);
        }

        public ThreatInfo GetThreatInfo(GameEntity entity)
        {
            return threatEvaluator.GetThreat(entity);
        }

        public bool HasDetected(GameEntity entity)
        {
            return threatEvaluator.HasThreat(entity);
        }

        public bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            return !Physics.Linecast(from, to, losMask);
        }

        private void HandleSensorDetection(GameEntity entity, float strength)
        {
            if (!entity || strength <= 0f || !IsEnemy(entity))
                return;

            if (!currentCycleDetections.TryAdd(entity, strength))
                currentCycleDetections[entity] += strength;
        }

        private void HandleHitDetection(GameEntity attacker, float strength, DamageDeliveryType deliveryType)
        {
            if (attacker == null || strength <= 0f || !IsEnemy(attacker))
                return;

            threatEvaluator.RecordHit(attacker, strength, deliveryType);
            OnHitDetected?.Invoke(attacker, strength);
        }

        public override void DestroyModule() { }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (currentCycleDetections == null || currentCycleDetections.Count == 0)
                return;

            GameEntity highestEntity = null;
            float highestScore = float.NegativeInfinity;

            foreach (var kvp in currentCycleDetections)
            {
                if (kvp.Key == null) continue;
                float score = GetThreatScore(kvp.Key);
                if (score > highestScore)
                {
                    highestScore = score;
                    highestEntity = kvp.Key;
                }
            }

            Vector3 origin = transform.position;

            foreach (var kvp in currentCycleDetections)
            {
                GameEntity entity = kvp.Key;
                if (entity == null) continue;

                bool isPrimary = entity == highestEntity;

                Gizmos.color = isPrimary ? Color.red : Color.yellow;
                Gizmos.DrawLine(origin, entity.transform.position);

                if (isPrimary)
                    Gizmos.DrawSphere(entity.transform.position, 0.3f);

                Vector3 labelPos = entity.transform.position + Vector3.up * 1.5f;
                Handles.Label(labelPos, $"V:{kvp.Value:F2}");
            }
        }
#endif
    }
}
