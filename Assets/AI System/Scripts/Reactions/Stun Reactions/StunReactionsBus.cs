using System;
using UnityEngine;

namespace Artifika.AI.Stun.Reactions
{
    [DisallowMultipleComponent]
    public class StunReactionsBus : ReactionBusBase
    {
        private EntityStatsController entityStatsController;
        private Blackboard blackboard;
        private IStunReaction[] reactions;

        public override void Initialize(Blackboard blackboard)
        {
            this.blackboard = blackboard;
            entityStatsController = this.blackboard.entityStatsController;

            reactions = this.blackboard.GetComponentsInChildren<IStunReaction>(true);
            foreach (var reaction in reactions)
            {
                reaction.Initialize(this.blackboard);
            }

            if (entityStatsController != null)
            {
                entityStatsController.OnStunnedChanged += HandleStunnedChanged;
                if (entityStatsController.IsStunned)
                {
                    HandleStunnedChanged(true);
                }
            }
        }

        private void OnDisable()
        {
            if (entityStatsController != null)
            {
                entityStatsController.OnStunnedChanged -= HandleStunnedChanged;
            }
        }

        private void HandleStunnedChanged(bool isStunned)
        {
            foreach (var reaction in reactions)
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
