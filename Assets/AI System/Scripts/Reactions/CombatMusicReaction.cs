using Artifika.Audio.Musics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class CombatMusicReaction : MonoBehaviour
    {
        [SerializeField] private MusicTrack combatMusic;
        [SerializeField] private AggroTracker aggroTracker;

        private bool hasRequestedCombatMusic;

        private void OnEnable()
        {
            if (aggroTracker == null)
                return;

            aggroTracker.OnCombatStarted += HandleCombatStarted;
            aggroTracker.OnCombatEnded += HandleCombatEnded;
        }

        private void Start()
        {
            if (aggroTracker != null && aggroTracker.IsInCombat)
                HandleCombatStarted();
        }

        private void OnDisable()
        {
            if (aggroTracker == null)
                return;

            aggroTracker.OnCombatStarted -= HandleCombatStarted;
            aggroTracker.OnCombatEnded -= HandleCombatEnded;
            HandleCombatEnded();
        }

        private void HandleCombatStarted()
        {
            if (!CanControlLocalMusic() || hasRequestedCombatMusic || MusicManager.Instance == null)
                return;

            MusicManager.Instance.Request(combatMusic);
            hasRequestedCombatMusic = true;
        }

        private void HandleCombatEnded()
        {
            if (!hasRequestedCombatMusic)
                return;

            MusicManager.Instance?.Release(combatMusic);
            hasRequestedCombatMusic = false;
        }

        private bool CanControlLocalMusic()
        {
            return aggroTracker != null
                   && combatMusic != null
                   && (!aggroTracker.IsSpawned || aggroTracker.IsOwner);
        }
    }
}
