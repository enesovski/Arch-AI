using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EntitySpawner : NetworkBehaviour
{
    [SerializeField] public Bounds spawnZone;
    [SerializeField] private List<SpawnData> entitySpawnDatas = new List<SpawnData>();
    [SerializeField] private LayerMask terrainLayer; // Layer mask to identify the terrain
    public Terrain terrain;

    private Dictionary<SpawnData, int> currentSpawnCounts;

    public enum SpawnerTypes { OnTerrain, InsideSpawnZone }

    [EnumToggleButtons, HideLabel]
    public SpawnerTypes spawnerType = SpawnerTypes.InsideSpawnZone;

    [Title("Settings")]
    [ShowIf("spawnerType", SpawnerTypes.OnTerrain)]
    public float minSpawnableHeight;

    [ShowIf("spawnerType", SpawnerTypes.OnTerrain)]
    public float maxSpawnableHeight;

    public void StartSpawning()
    {
        if (!IsServer) return; // Ensure this only runs on the server

        currentSpawnCounts = new Dictionary<SpawnData, int>();

        foreach (var spawnData in entitySpawnDatas)
        {
            currentSpawnCounts[spawnData] = 0;

            // NEW: initial wave (designated count, else fallback to maxCount)
            int desired = spawnData.initialCount > 0 ? spawnData.initialCount : spawnData.maxCount;
            int target = Mathf.Clamp(desired, 0, spawnData.maxCount);

            if (target > 0)
            {
                SpawnInitialWave(spawnData, target);
            }

            // Continue timed spawning as before
            StartCoroutine(SpawnEntity(spawnData));
        }

    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnEntity(SpawnData spawnData)
    {
            while (true)
            {
                yield return new WaitForSeconds(60f / spawnData.spawnRate);

                if (currentSpawnCounts[spawnData] >= spawnData.maxCount)
                    continue;

                TrySpawnOnce(spawnData);
            }
    }

    private void SpawnInitialWave(SpawnData spawnData, int target)
    {
        int attempts = 0;
        int attemptCap = Mathf.Max(50, target * 10); 
        while (currentSpawnCounts[spawnData] < target && attempts < attemptCap)
        {
            attempts++;
            TrySpawnOnce(spawnData);
        }
    }
    
    private void TrySpawnOnce(SpawnData spawnData)
    {
        Vector3 randomPosition = GetRandomPositionInBounds(spawnZone);

        if (!IsOnTerrain(randomPosition, out var terrainPosition))
            return;

        if (spawnerType == SpawnerTypes.OnTerrain)
        {
            if (terrainPosition.y > maxSpawnableHeight || terrainPosition.y < minSpawnableHeight)
                return;
        }

        if (!IsOnNavmesh(terrainPosition))
            return;

        var spawnedEntity = Instantiate(spawnData.animalData.prefab, terrainPosition, Quaternion.identity);
        
        spawnedEntity.GetComponent<NetworkObject>().Spawn();

        currentSpawnCounts[spawnData]++;

        var livingEntity = spawnedEntity.GetComponent<ILivingEntity>();
        if (livingEntity != null)
        {
            livingEntity.OnDeath += () => currentSpawnCounts[spawnData]--;
        }
    }

    private Vector3 GetRandomPositionInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.max.y, 
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private bool IsOnTerrain(Vector3 position, out Vector3 terrainPosition)
    {
        terrainPosition = Vector3.zero; 

        if (Physics.Raycast(position + Vector3.up * 1000, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainLayer))
        {
            terrainPosition = hit.point; // Set the terrain position to the hit point
            return true; // Return true if the terrain was hit by the ray
        }

        return false; // Return false if the terrain was not hit
    }

    private bool IsOnNavmesh(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 2.5f, NavMesh.AllAreas))
        {
            return true;
        }
        return false;
    }
}

[System.Serializable]
public struct SpawnData
{
    public BaseAnimalData animalData;
    public float spawnRate; // Creatures per minute
    public int maxCount;    // Max number of this creature on the map

    [Min(0), Tooltip("Initial spawn count at StartSpawning(). 0 = use maxCount.")]
    public int initialCount; // NEW: designated initial count (fallback to maxCount when 0)
}
