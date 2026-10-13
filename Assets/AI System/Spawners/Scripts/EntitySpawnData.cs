using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCreatureSpawnData", menuName = "NPC Data/Creature Spawn Data", order = 1)]
public class EntitySpawnData : ScriptableObject
{
    public GameObject creaturePrefab;
    public float spawnRate; // Creatures per minute
    public int maxCount; // Max number of this creature on the map
}
