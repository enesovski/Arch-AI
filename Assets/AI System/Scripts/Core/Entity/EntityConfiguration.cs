using Artifika.AI;
using Sirenix.OdinInspector;
using UnityEngine;

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

    [HideIf("baseEntityType", EntityType.Boss)]
    [BoxGroup("Attributes/Taming", ShowLabel = true)]
    public bool isTameable;

    [BoxGroup("Attributes/Taming")]
    [ShowIf("@this.isTameable && this.baseEntityType != Artifika.AI.EntityType.Boss")]
    public GameObject tamedPrefab;

    [BoxGroup("Attributes/Taming")]
    [ShowIf("@this.isTameable && this.baseEntityType != Artifika.AI.EntityType.Boss")]
    public OverrideSkillCheckSettings overrideSkillChecks = OverrideSkillCheckSettings.PrototypeDefault;

    [BoxGroup("Attributes/Combat Response")]
    [ShowIf("baseEntityType", EntityType.Passive)]
    public bool rangedHitForcesEngage = true;
    
    [ShowIf("baseEntityType", EntityType.Boss)]
    [BoxGroup("Attributes/Boss", ShowLabel = true)]
    public string bossDisplayName;
    
    [Title("Drop Table")]
    [TableList(AlwaysExpanded = true)]
    public DropData[] potentialDrops;
}
