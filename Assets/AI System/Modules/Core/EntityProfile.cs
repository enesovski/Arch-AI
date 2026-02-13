using UnityEngine;

[CreateAssetMenu(fileName = "EntityProfile", menuName = "AI/EntityProfile")]
public class EntityProfile : ScriptableObject
{
    public string entityName;
    public string entityId;
    [TextArea]
    public string description;
}

