using System;
using System.Linq;
using UnityEngine;

namespace Artifika.AI.Aggro.Reactions
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
                aggroModule.OnStateChanged += HandleStateChanged;

            reactions = GetComponents<IAggroReaction>();
            hasExplicitRotationReaction = reactions.Any(r => r is AggroRotationReaction);

            foreach (var reaction in reactions)
                reaction.Initialize(this.blackboard);
        }

        private void OnDisable()
        {
            if (aggroModule != null)
                aggroModule.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(AggroStateChangeEventArgs args)
        {
            foreach (var reaction in reactions)
            {
                try
                {
                    reaction.OnAggroStateChanged(args);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }

            ApplyFallbackRotation(args.NewState);
        }

        private void ApplyFallbackRotation(AggroState state)
        {
            if (hasExplicitRotationReaction || rotationModule == null)
                return;

            rotationModule.SetMode(RotationMode.Agent);
        }
    }
}
