using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Death.Reactions
{
    [DisallowMultipleComponent]
    public class DeathReactionsBus : ReactionBusBase
    {
        private HealthComponent healthComponent;
        private Blackboard _blackboard;
        
        [Title("Reactions")]
        [SerializeField] private Transform onDeathReactionsParent;
        [SerializeField] private Transform onDespawnReactionsParent;
        
        private IDeathReaction[] onDeathReactions;
        private IDeathReaction[] onDespawnReactions;
        
        public override void Initialize(Blackboard blackboard)
        {
            this._blackboard = blackboard;
            healthComponent = blackboard != null ? blackboard.healthComponent : null;
            
            onDeathReactions = onDeathReactionsParent != null
                ? onDeathReactionsParent.GetComponents<IDeathReaction>()
                : Array.Empty<IDeathReaction>();
            InitializeReactions(onDeathReactions, blackboard);
            
            if (healthComponent != null)
                healthComponent.OnDeath += ExecuteOnDeathReactions;

            onDespawnReactions = onDespawnReactionsParent != null
                ? onDespawnReactionsParent.GetComponents<IDeathReaction>()
                : Array.Empty<IDeathReaction>();
            InitializeReactions(onDespawnReactions, blackboard);
        }

        private void OnDisable()
        {
            if (healthComponent != null)
                healthComponent.OnDeath -= ExecuteOnDeathReactions;
        }
        

        private void ExecuteOnDeathReactions()
        {
            if (onDeathReactions == null)
                return;

            foreach (var r in onDeathReactions)
            {
                if (r == null)
                    continue;

                try { r.Execute(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        public void ExecuteOnDespawnReactions()
        {
            EnsureOnDespawnReactionsCached();

            foreach (var r in onDespawnReactions)
            {
                if (r == null)
                    continue;

                try { r.Execute(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        private void EnsureOnDespawnReactionsCached()
        {
            if (onDespawnReactions != null)
                return;

            onDespawnReactions = onDespawnReactionsParent != null
                ? onDespawnReactionsParent.GetComponents<IDeathReaction>()
                : Array.Empty<IDeathReaction>();
        }

        private static void InitializeReactions(IDeathReaction[] reactions, Blackboard blackboard)
        {
            if (reactions == null)
                return;

            foreach (IDeathReaction reaction in reactions)
            {
                if (reaction is IDeathReactionInitializable initializable)
                    initializable.Initialize(blackboard);
            }
        }
    }
}
