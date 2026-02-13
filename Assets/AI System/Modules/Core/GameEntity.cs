using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using Blackboard = Artifika.AI.Blackboard;

[DisallowMultipleComponent]
public class GameEntity : MonoBehaviour
{
    [Title("Identity"), InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Boxed), AssetsOnly]
    public EntityProfile entityProfile;
    public FactionType Faction;
    protected List<GameEntity> aggroEntities = new List<GameEntity>(8);
    public Blackboard Blackboard { get; private set; }
    public HealthComponent healthComponent { get; private set; }

    private void Start()
    {
        Blackboard = GetComponent<Blackboard>();
        healthComponent = GetComponent<HealthComponent>();
    }

    public virtual void RegisterAggro(GameEntity entity)
    {
        if (!entity) return;
        if (aggroEntities.Contains(entity)) return;

        aggroEntities.Add(entity);

    }
    public virtual void UnregisterAggro(GameEntity entity)
    {
        if (!entity) return;
        if (!aggroEntities.Contains(entity)) return;

        aggroEntities.Remove(entity);
    }

    public string GetAnalyticsDisplayId()
    {
        return entityProfile.entityName;
    }
}
