using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Artifika.AI.Sensors;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artifika.AI
{
    public class DetectionModule : UpdatableEntityModule
    {
        public const int MAX_TARGET_COUNT = 16;

        [Title("Faction")]
        [SerializeField] private List<FactionType> enemyFactions;

        private float detectionTimeInterval;

        [Title("Threat Evaluation")]
        [SerializeField] private ThreatEvaluationSettings threatSettings;

        public event Action<GameEntity, float> OnDetectionUpdate;
        public event Action<GameEntity, float> OnHitDetected;
        public event Action<ThreatEvaluation> OnThreatEvaluated;

        private List<BaseSensor> sensorComponents;
        private ThreatEvaluator threatEvaluator;
        private Coroutine updateCoroutine;

        private readonly Dictionary<GameEntity, float> currentCycleDetections = new Dictionary<GameEntity, float>();

        protected override void OnInitialize()
        {
            threatEvaluator = new ThreatEvaluator(threatSettings);
            sensorComponents = new List<BaseSensor>(GetComponentsInChildren<BaseSensor>());

            detectionTimeInterval = GetComponent<EntityCoordinator>().GetUpdateInterval();

            foreach (var sensor in sensorComponents)
            {
                sensor.Initialize(transform);
                
                if (sensor is HitSensor)
                    sensor.OnDetected += HandleHitDetection;
                else
                    sensor.OnDetected += HandleSensorDetection;
            }
        }

        public override void PerformUpdate(float deltaTime)
        {
            currentCycleDetections.Clear();

            foreach (var sensor in sensorComponents)
            {
                if (sensor is HitSensor)
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

            ThreatEvaluation evaluation = threatEvaluator.Evaluate();
            OnThreatEvaluated?.Invoke(evaluation);
        }

        #region Threat

        public void GetThreatEntities(List<GameEntity> results)
        {
            threatEvaluator.GetThreatEntities(results);
        }

        public float GetThreatScore(GameEntity entity)
        {
            ThreatInfo threat = threatEvaluator.GetThreat(entity);
            if (threat == null)
                return 0f;

            return threatEvaluator.CalculateThreatScore(threat);
        }

        public ThreatInfo GetThreatInfo(GameEntity entity)
        {
            return threatEvaluator.GetThreat(entity);
        }

        public bool HasDetected(GameEntity entity)
        {
            return threatEvaluator.HasThreat(entity);
        }

        #endregion

        #region Sensor Handlers

        private void HandleSensorDetection(GameEntity entity, float strength)
        {
            if (!entity || strength <= 0f || !enemyFactions.Contains(entity.Faction))
                return;

            if (!currentCycleDetections.TryAdd(entity, strength))
                currentCycleDetections[entity] += strength;
        }

        private void HandleHitDetection(GameEntity attacker, float strength)
        {
            if (attacker == null || strength <= 0f || !enemyFactions.Contains(attacker.Faction))
                return;

            threatEvaluator.RecordHit(attacker, strength);
            OnHitDetected?.Invoke(attacker, strength);

            ThreatEvaluation evaluation = threatEvaluator.Evaluate();
            OnThreatEvaluated?.Invoke(evaluation);
        }

        #endregion

        protected override void OnDisable()
        {
            base.OnDisable();
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }

            foreach (var sensor in sensorComponents)
            {
                if (!sensor) continue;

                if (sensor is HitSensor)
                    sensor.OnDetected -= HandleHitDetection;
                else
                    sensor.OnDetected -= HandleSensorDetection;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (currentCycleDetections == null || currentCycleDetections.Count == 0)
                return;

            Vector3 origin = transform.position;
            var evaluation = threatEvaluator?.Evaluate();

            foreach (var kvp in currentCycleDetections)
            {
                GameEntity entity = kvp.Key;
                if (entity == null) continue;

                bool isCurrentThreat = evaluation.HasValue && evaluation.Value.Entity == entity;

                Gizmos.color = isCurrentThreat ? Color.red : Color.yellow;
                Gizmos.DrawLine(origin, entity.transform.position);

                if (isCurrentThreat)
                    Gizmos.DrawSphere(entity.transform.position, 0.3f);

                Vector3 labelPos = entity.transform.position + Vector3.up * 1.5f;
                Handles.Label(labelPos, $"V:{kvp.Value:F2}");
            }
        }
#endif

        public override void DestroyModule()
        {
        }
    }
}