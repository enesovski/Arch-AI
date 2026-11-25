using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "New Entity Configuration", menuName = "AI/Entity Configuration")]
    public class EntityConfiguration : ScriptableObject
    {
        [Title("Identity")]
        public string entityName;
        public string entityId;
        [TextArea]
        public string description;

        [Title("Prefab")]
        public GameObject prefab;

        [Title("Attributes")]
        [VerticalGroup("Attributes")]
        [HideLabel]
        public EntityType baseEntityType = EntityType.Aggressive;

        [BoxGroup("Attributes/Combat Response")]
        [ShowIf("baseEntityType", EntityType.Passive)]
        public bool rangedHitForcesEngage = true;
    }
}
