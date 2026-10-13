using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public class CompanionStateReactionsBus : ReactionBusBase
    {
        private CompanionStateModule companionStateModule;
        private Blackboard blackboard;

        private ICompanionReaction[] reactions;

        public void Initialize(Blackboard blackboard)
        {
            this.blackboard = blackboard;

            this.companionStateModule = this.blackboard.companionStateModule;

            if (companionStateModule)
            {
                companionStateModule.OnStateChanged += HandleStateChanged;
            }

            reactions = GetComponents<ICompanionReaction>();
            foreach (var reaction in reactions)
            {
                reaction.Initialize(this.blackboard);
            }
        }

        private void OnDisable()
        {
            if (companionStateModule != null)
            {
                companionStateModule.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(CompanionState previousState, CompanionState nextState)
        {
            if (reactions == null) return;

            foreach (var reaction in reactions)
            {
                try
                {
                    reaction.OnCompanionStateChanged(previousState, nextState);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }
    }
}
