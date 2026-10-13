using System;
using System.Linq;
using UnityEngine;

namespace Artifika.AI.Aggro.Reactions
{
    [DisallowMultipleComponent]
    public class AggroReactionsBus : ReactionBusBase
    {
        private AggroModule _aggroModule;
        private Blackboard _blackboard;
        private RotationModule rotationModule;
        private bool hasExplicitRotationReaction;

        private IAggroReaction[] reactions;

        public override void Initialize(Blackboard blackboard)
        {
            _blackboard = blackboard;
            _aggroModule = _blackboard.aggroModule;
            rotationModule = _blackboard.rotationModule;

            if (_aggroModule)
                _aggroModule.OnStateChanged += HandleStateChanged;

            reactions = GetComponents<IAggroReaction>();
            hasExplicitRotationReaction = reactions.Any(r => r is AggroRotationReaction);

            foreach (var reaction in reactions)
                reaction.Initialize(_blackboard);
        }

        private void OnDisable()
        {
            if (_aggroModule != null)
                _aggroModule.OnStateChanged -= HandleStateChanged;
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
