using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class EntitySpawner : NetworkBehaviour
    {
        [SerializeField] public Vector3 zoneSize = new Vector3(10f, 20f, 10f);

        [SerializeField] private List<SpawnData> entitySpawnDatas = new List<SpawnData>();
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private LayerMask blockingLayers;
        public Terrain terrain;

        public LayerMask TerrainLayerMask => terrainLayer;

        [FormerlySerializedAs("_legacySpawnZone")]
        [SerializeField, HideInInspector] private Bounds legacySpawnZone;
        [FormerlySerializedAs("_migrated")]
        [SerializeField, HideInInspector] private bool migrated;

        private Dictionary<SpawnData, int> currentSpawnCounts;
        private readonly List<(HealthComponent entity, Action callback)> deathCallbacks
            = new List<(HealthComponent, Action)>();

        public enum SpawnerTypes { OnTerrain, InsideSpawnZone }

        [EnumToggleButtons, HideLabel]
        public SpawnerTypes spawnerType = SpawnerTypes.InsideSpawnZone;

        [Title("Settings")]
        [ShowIf("spawnerType", SpawnerTypes.OnTerrain)]
        public float minSpawnableHeight;

        [ShowIf("spawnerType", SpawnerTypes.OnTerrain)]
        public float maxSpawnableHeight;

        private void OnValidate()
        {
            // Preserve zone dimensions serialized by the previous Bounds representation.
            if (!migrated && legacySpawnZone.size != Vector3.zero)
            {
                zoneSize = legacySpawnZone.size;
                legacySpawnZone = default;
                migrated = true;
    #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
    #endif
                Debug.Log($"[EntitySpawner] '{gameObject.name}' migrated legacy Bounds to zoneSize: {zoneSize}");
            }

            zoneSize.x = Mathf.Max(0.1f, zoneSize.x);
            zoneSize.y = Mathf.Max(0.1f, zoneSize.y);
            zoneSize.z = Mathf.Max(0.1f, zoneSize.z);
        }

        public void StartSpawning()
        {
            if (!IsServer) return;

            if (spawnerType == SpawnerTypes.OnTerrain && terrain == null)
            {
                GameLog.Error($"[EntitySpawner] '{gameObject.name}' is OnTerrain mode but has no Terrain assigned. Spawning aborted.");
                return;
            }

            GameLog.Info($"[EntitySpawner] StartSpawning on '{gameObject.name}' — mode: {spawnerType}, types: {entitySpawnDatas.Count}");

            currentSpawnCounts = new Dictionary<SpawnData, int>();

            foreach (var spawnData in entitySpawnDatas)
            {
                if (spawnData.animalData == null)
                {
                    GameLog.Warning($"[EntitySpawner] '{gameObject.name}' has a SpawnData entry with no AnimalData assigned. Skipping.");
                    continue;
                }

                currentSpawnCounts[spawnData] = 0;

                int desired = spawnData.initialCount > 0 ? spawnData.initialCount : spawnData.maxCount;
                int target = Mathf.Clamp(desired, 0, spawnData.maxCount);

                if (target > 0)
                    StartCoroutine(SpawnInitialWaveCoroutine(spawnData, target));

                if (spawnData.spawnRate > 0f)
                    StartCoroutine(SpawnTickCoroutine(spawnData));
                else
                    GameLog.Info($"[EntitySpawner] '{spawnData.animalData.name}' spawnRate is 0 — initial wave only.");
            }
        }

        public void StopSpawning()
        {
            StopAllCoroutines();

            foreach (var (entity, cb) in deathCallbacks)
            {
                if (entity != null)
                    entity.OnDeath -= cb;
            }
            deathCallbacks.Clear();
        }

        private IEnumerator SpawnTickCoroutine(SpawnData spawnData)
        {
            float interval = 60f / spawnData.spawnRate;

            while (true)
            {
                yield return new WaitForSeconds(interval);

                if (currentSpawnCounts[spawnData] < spawnData.maxCount)
                    TrySpawnOnce(spawnData);
            }
        }

        private IEnumerator SpawnInitialWaveCoroutine(SpawnData spawnData, int target)
        {
            // Limit raycast work per frame during the initial wave.
            const int AttemptsPerFrame = 5;
            int attempts = 0;
            int attemptCap = Mathf.Max(50, target * 10);

            while (currentSpawnCounts[spawnData] < target && attempts < attemptCap)
            {
                for (int i = 0;
                     i < AttemptsPerFrame
                     && currentSpawnCounts[spawnData] < target
                     && attempts < attemptCap;
                     i++)
                {
                    attempts++;
                    TrySpawnOnce(spawnData);
                }
                yield return null;
            }

            if (currentSpawnCounts[spawnData] < target)
            {
                GameLog.Warning($"[EntitySpawner] '{gameObject.name}' initial wave for '{spawnData.animalData.name}' " +
                                $"reached only {currentSpawnCounts[spawnData]}/{target} after {attempts} attempts. " +
                                $"Check spawn zone placement, terrain layer mask, and NavMesh coverage.");
            }
            else
            {
                GameLog.Info($"[EntitySpawner] '{gameObject.name}' initial wave complete for '{spawnData.animalData.name}': " +
                             $"{currentSpawnCounts[spawnData]}/{target} spawned in {attempts} attempts.");
            }
        }

        private void TrySpawnOnce(SpawnData spawnData)
        {
            Vector3 sampleOrigin = spawnerType == SpawnerTypes.OnTerrain
                ? GetRandomWorldPositionOnTerrain()
                : GetRandomWorldPositionInZone();

            if (sampleOrigin == Vector3.zero) return;

            GameLog.Info($"[EntitySpawner] Attempting '{spawnData.animalData.name}' at {sampleOrigin} " +
                         $"({currentSpawnCounts[spawnData] + 1}/{spawnData.maxCount})");

            if (!IsOnTerrain(sampleOrigin, out Vector3 terrainPosition))
            {
                GameLog.Warning($"[EntitySpawner] No terrain found under {sampleOrigin} for '{spawnData.animalData.name}'.");
                return;
            }

            if (spawnerType == SpawnerTypes.OnTerrain)
            {
                if (terrainPosition.y > maxSpawnableHeight || terrainPosition.y < minSpawnableHeight)
                {
                    GameLog.Warning($"[EntitySpawner] Height {terrainPosition.y:F1} outside " +
                                    $"[{minSpawnableHeight:F1}, {maxSpawnableHeight:F1}] " +
                                    $"for '{spawnData.animalData.name}'.");
                    return;
                }
            }

            if (!IsOnNavmesh(terrainPosition))
            {
                GameLog.Warning($"[EntitySpawner] No NavMesh at {terrainPosition} for '{spawnData.animalData.name}'.");
                return;
            }

            var spawnedGO = Instantiate(spawnData.animalData.prefab, terrainPosition, Quaternion.identity);
            spawnedGO.GetComponent<NetworkObject>().Spawn();

            currentSpawnCounts[spawnData]++;

            var livingEntity = spawnedGO.GetComponent<HealthComponent>();
            if (livingEntity != null)
            {
                var key = spawnData;
                Action onDeath = null;
                onDeath = () =>
                {
                    if (currentSpawnCounts != null && currentSpawnCounts.ContainsKey(key))
                        currentSpawnCounts[key]--;
                };
                livingEntity.OnDeath += onDeath;
                deathCallbacks.Add((livingEntity, onDeath));
            }

            GameLog.Info($"[EntitySpawner] Spawned '{spawnData.animalData.name}' at {terrainPosition}. " +
                         $"Count: {currentSpawnCounts[spawnData]}/{spawnData.maxCount}");
        }

        private Vector3 GetRandomWorldPositionInZone()
        {
            var localPoint = new Vector3(
                UnityEngine.Random.Range(-zoneSize.x * 0.5f, zoneSize.x * 0.5f),
                zoneSize.y * 0.5f,
                UnityEngine.Random.Range(-zoneSize.z * 0.5f, zoneSize.z * 0.5f)
            );
            return transform.TransformPoint(localPoint);
        }

        private Vector3 GetRandomWorldPositionOnTerrain()
        {
            if (terrain == null)
            {
                GameLog.Error($"[EntitySpawner] '{gameObject.name}' terrain reference is null during spawn attempt.");
                return Vector3.zero;
            }

            Vector3 tp = terrain.transform.position;
            Vector3 td = terrain.terrainData.size;

            float x = UnityEngine.Random.Range(tp.x, tp.x + td.x);
            float z = UnityEngine.Random.Range(tp.z, tp.z + td.z);

            float y = tp.y + td.y + 500f;

            return new Vector3(x, y, z);
        }

        private bool IsOnTerrain(Vector3 worldPosition, out Vector3 terrainPosition)
        {
            terrainPosition = Vector3.zero;
            // A fixed altitude keeps low spawn zones from starting below the terrain.
            const float CastFromAltitude = 5000f;
            var origin = new Vector3(worldPosition.x, CastFromAltitude, worldPosition.z);

            LayerMask combinedMask = terrainLayer | blockingLayers;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Mathf.Infinity, combinedMask))
            {
                if (blockingLayers != 0 && (blockingLayers & (1 << hit.collider.gameObject.layer)) != 0)
                    return false;

                terrainPosition = hit.point;
                return true;
            }
            return false;
        }

        private bool IsOnNavmesh(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out _, 2.5f, NavMesh.AllAreas);
        }

    #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (spawnerType == SpawnerTypes.OnTerrain && terrain != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Vector3 tp = terrain.transform.position;
                Vector3 td = terrain.terrainData.size;
                Vector3 center = tp + td * 0.5f;

                Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
                Gizmos.DrawWireCube(new Vector3(center.x, minSpawnableHeight, center.z), new Vector3(td.x, 0f, td.z));
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
                Gizmos.DrawWireCube(new Vector3(center.x, maxSpawnableHeight, center.z), new Vector3(td.x, 0f, td.z));
            }
        }
    #endif
    }

    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [System.Serializable]
    public struct SpawnData : IEquatable<SpawnData>
    {
        public EntityConfiguration animalData;

        [Min(0f), Tooltip("Spawns per minute. 0 = initial wave only, no timed respawning.")]
        public float spawnRate;

        [Min(1)]
        public int maxCount;

        [Min(0), Tooltip("How many to spawn immediately at StartSpawning(). 0 = use maxCount.")]
        public int initialCount;

        public bool Equals(SpawnData other) =>
            ReferenceEquals(animalData, other.animalData) &&
            Mathf.Approximately(spawnRate, other.spawnRate) &&
            maxCount == other.maxCount &&
            initialCount == other.initialCount;

        public override bool Equals(object obj) => obj is SpawnData sd && Equals(sd);

        public override int GetHashCode() => HashCode.Combine(animalData, spawnRate, maxCount, initialCount);
    }
}
