using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "NewCreatureSpawnData", menuName = "NPC Data/Creature Spawn Data", order = 1)]
    public class EntitySpawnData : ScriptableObject
    {
        public GameObject creaturePrefab;
        public float spawnRate; // Creatures per minute
        public int maxCount;
    }
}
