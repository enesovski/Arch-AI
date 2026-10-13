using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "NewCreatureSpawnData", menuName = "NPC Data/Creature Spawn Data", order = 1)]
    public class EntitySpawnData : ScriptableObject
    {
        public GameObject creaturePrefab;
        public float spawnRate; // Creatures per minute
        public int maxCount;
    }
}
