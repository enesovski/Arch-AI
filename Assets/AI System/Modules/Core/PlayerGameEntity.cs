using Artifika.Audio;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerGameEntity : GameEntity
{
    [Title("Player")]
    [SerializeField] MusicTrack combatMusic;

    public override void RegisterAggro(GameEntity entity)
    {
        base.RegisterAggro(entity);

        if(aggroEntities.Count == 1)
        {
            MusicManager.Instance.Request(combatMusic);
        }
    }

    public override void UnregisterAggro(GameEntity entity)
    {
        base.UnregisterAggro(entity);

        if (aggroEntities.Count == 0)
        {
            MusicManager.Instance.Release(combatMusic);
        }
    }


}
