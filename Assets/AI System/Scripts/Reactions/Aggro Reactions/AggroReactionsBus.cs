using System;
using System.Linq;
using UnityEngine;

namespace Archura.Aggro.Reactions
{
    [DisallowMultipleComponent]
    public class AggroReactionsBus : ReactionBusBase
    {
        private AggroModule aggroModule;
        private Blackboard blackboard;
        private RotationModule rotationModule;
        private bool hasExplicitRotationReaction;

        private IAggroReaction[] reactions;

        public override void Initialize(Blackboard blackboard)
        {
            this.blackboard = blackboard;
            aggroModule = this.blackboard.aggroModule;
            rotationModule = this.blackboard.rotationModule;

            if (aggroModule)
                aggroModule.OnEngagementChanged += HandleEngagementChanged;

            reactions = GetComponents<IAggroReaction>();
            hasExplicitRotationReaction = reactions.Any(r => r is AggroRotationReaction);

            foreach (var reaction in reactions)
                reaction.Initialize(this.blackboard);
        }

        private void OnDisable()
        {
            if (aggroModule != null)
                aggroModule.OnEngagementChanged -= HandleEngagementChanged;
        }

        private void HandleEngagementChanged(EngagementChangeEventArgs args)
        {
            foreach (var reaction in reactions)
            {
                try
                {
                    reaction.OnEngagementChanged(args);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }

            if (args.ChangesAwareness)
                ApplyFallbackRotation();
        }

        private void ApplyFallbackRotation()
        {
            if (hasExplicitRotationReaction || !rotationModule)
                return;

            rotationModule.SetMode(RotationMode.Agent);
        }
    }
}
