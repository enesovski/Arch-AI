using System;
using UnityEngine;

namespace Artifika.AI.Stun.Reactions
{
    [DisallowMultipleComponent]
    public class StunReactionsBus : ReactionBusBase
    {
        private EntityStatsController _entityStatsController;
        private Blackboard _blackboard;
        private IStunReaction[] _reactions;

        public override void Initialize(Blackboard blackboard)
        {
            _blackboard = blackboard;
            _entityStatsController = _blackboard.entityStatsController;
            
            _reactions = _blackboard.GetComponentsInChildren<IStunReaction>(true);
            foreach (var reaction in _reactions)
            {
                reaction.Initialize(_blackboard);
            }

            if (_entityStatsController != null)
            {
                _entityStatsController.OnStunnedChanged += HandleStunnedChanged;
                // Catch up current state if already initialized/stunned
                if (_entityStatsController.IsStunned)
                {
                    HandleStunnedChanged(true);
                }
            }
        }

        private void OnDisable()
        {
            if (_entityStatsController != null)
            {
                _entityStatsController.OnStunnedChanged -= HandleStunnedChanged;
            }
        }

        private void HandleStunnedChanged(bool isStunned)
        {
            foreach (var reaction in _reactions)
            {
                try
                {
                    reaction.OnStunnedChanged(isStunned);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }
    }
}
