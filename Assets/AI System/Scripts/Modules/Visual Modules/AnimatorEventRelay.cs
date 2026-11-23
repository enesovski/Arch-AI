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
        private const string HarvestInstantEvent = "AE_HarvestInstant";

        [Tooltip("Component implementing " + nameof(IAttackAnimationEventSource) + ". Resolved from parents when empty.")]
        [FormerlySerializedAs("animatorEventHandler")]
        [SerializeField] private MonoBehaviour attackEventSource;
        [SerializeField] private VisualEffectsModule visualEffectsModule;
        [Tooltip("Optional extra component implementing " + nameof(IAnimationEffectPlayer) + ".")]
        [FormerlySerializedAs("animatorEffectPlayer")]
        [SerializeField] private MonoBehaviour effectPlayer;
        [SerializeField] private DeathReactionsBus deathReactionsBus;
        [Tooltip("Optional component implementing " + nameof(IAnimationEventReceiver) + " for host-specific events.")]
        [FormerlySerializedAs("harvestAnimRelay")]
        [SerializeField] private MonoBehaviour eventReceiver;

        private IAttackAnimationEventSource AttackEvents => attackEventSource as IAttackAnimationEventSource;
        private IAnimationEffectPlayer EffectPlayer => effectPlayer as IAnimationEffectPlayer;
        private IAnimationEventReceiver EventReceiver => eventReceiver as IAnimationEventReceiver;

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
            visualEffectsModule?.PlayEffect(key);
            EffectPlayer?.PlayEffect(key);
        }

        public void AE_HarvestInstant()
        {
            ResolveTargets();
            EventReceiver?.OnAnimationEvent(HarvestInstantEvent);
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
            if (AttackEvents == null)
                attackEventSource = GetComponentInParent<IAttackAnimationEventSource>(true) as MonoBehaviour;

            if (visualEffectsModule == null)
                visualEffectsModule = GetComponentInParent<VisualEffectsModule>(true);

            if (EffectPlayer == null)
                effectPlayer = FindInParents<IAnimationEffectPlayer>(visualEffectsModule);

            if (deathReactionsBus == null)
                deathReactionsBus = GetComponentInParent<DeathReactionsBus>(true);
            if (EventReceiver == null)
                eventReceiver = GetComponentInParent<IAnimationEventReceiver>(true) as MonoBehaviour;
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
