using Artifika.AI.Attack;
using Artifika.AI.Death.Reactions;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public sealed class AnimatorEventRelay : MonoBehaviour
    {
        [SerializeField] private AnimatorEventHandler animatorEventHandler;
        [SerializeField] private VisualEffectsModule visualEffectsModule;
        [SerializeField] private AnimatorEffectPlayer animatorEffectPlayer;
        [SerializeField] private DeathReactionsBus deathReactionsBus;
        [SerializeField] private HarvestAnimRelay harvestAnimRelay;

        private void Awake()
        {
            ResolveTargets();
        }

        private void Reset()
        {
            ResolveTargets();
        }

        public void onAttackAnimationInvoke(BaseAttackDefinition attackDefinition)
        {
            ResolveTargets();
            animatorEventHandler?.onAttackAnimationInvoke(attackDefinition);
        }

        public void PlayEffect(string key)
        {
            ResolveTargets();
            visualEffectsModule?.PlayEffect(key);
            animatorEffectPlayer?.PlayEffect(key);
        }

        public void AE_HarvestInstant()
        {
            ResolveTargets();
            harvestAnimRelay?.AE_HarvestInstant();
        }

        public void AE_ExecuteOnDespawnReactions()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer)
                return;

            ResolveTargets();
            deathReactionsBus?.ExecuteOnDespawnReactions();
        }

        private void ResolveTargets()
        {
            if (animatorEventHandler == null)
                animatorEventHandler = GetComponentInParent<AnimatorEventHandler>(true);

            if (visualEffectsModule == null)
                visualEffectsModule = GetComponentInParent<VisualEffectsModule>(true);

            if (animatorEffectPlayer == null)
                animatorEffectPlayer = GetComponentInParent<AnimatorEffectPlayer>(true);

            if (deathReactionsBus == null)
                deathReactionsBus = GetComponentInParent<DeathReactionsBus>(true);
            if (harvestAnimRelay == null)
                harvestAnimRelay = GetComponentInParent<HarvestAnimRelay>(true);
        }
    }
}
