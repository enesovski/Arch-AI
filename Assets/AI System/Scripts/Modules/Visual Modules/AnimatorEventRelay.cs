using Archura.Attack;
using Archura.Death.Reactions;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public sealed class AnimatorEventRelay : MonoBehaviour
    {
        [Tooltip("Component implementing " + nameof(IAttackAnimationEventSource) + ". Resolved from parents when empty.")]
        [FormerlySerializedAs("animatorEventHandler")]
        [SerializeField] private MonoBehaviour attackEventSource;
        [SerializeField] private VisualEffectsModule visualEffectsModule;
        [Tooltip("Optional extra component implementing " + nameof(IAnimationEffectPlayer) + ".")]
        [FormerlySerializedAs("animatorEffectPlayer")]
        [SerializeField] private MonoBehaviour effectPlayer;
        [SerializeField] private DeathReactionsBus deathReactionsBus;

        private IAttackAnimationEventSource AttackEvents => attackEventSource as IAttackAnimationEventSource;
        private IAnimationEffectPlayer EffectPlayer => effectPlayer as IAnimationEffectPlayer;

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
            AttackEvents?.TriggerAttackAnimation(attackDefinition);
        }

        public void PlayEffect(string key)
        {
            ResolveTargets();
            if (visualEffectsModule)
                visualEffectsModule.PlayEffect(key);
            EffectPlayer?.PlayEffect(key);
        }

        public void AE_ExecuteOnDespawnReactions()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer)
                return;

            ResolveTargets();
            if (deathReactionsBus)
                deathReactionsBus.ExecuteOnDespawnReactions();
        }

        private void ResolveTargets()
        {
            if (AttackEvents == null)
                attackEventSource = GetComponentInParent<IAttackAnimationEventSource>(true) as MonoBehaviour;

            if (visualEffectsModule == null)
                visualEffectsModule = GetComponentInParent<VisualEffectsModule>(true);

            if (EffectPlayer == null)
                effectPlayer = FindInParents<IAnimationEffectPlayer>(visualEffectsModule);

            if (deathReactionsBus == null)
                deathReactionsBus = GetComponentInParent<DeathReactionsBus>(true);
        }

        // First parent component of type T other than the one already handled explicitly.
        private MonoBehaviour FindInParents<T>(Object exclude) where T : class
        {
            foreach (T candidate in GetComponentsInParent<T>(true))
            {
                if (candidate is MonoBehaviour behaviour && behaviour != exclude)
                    return behaviour;
            }
            return null;
        }
    }
}
