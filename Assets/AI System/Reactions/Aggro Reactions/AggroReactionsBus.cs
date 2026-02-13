using System;
using UnityEngine;

namespace Artifika.AI.Aggro.Reactions
{
    [DisallowMultipleComponent]
    public class AggroReactionsBus : MonoBehaviour, IReactionBus
    {
        private AggroModule _aggroModule;
        private Blackboard _blackboard;
        
        private IAggroReaction[] reactions;

        public void Initialize(Blackboard blackboard)
        {
            this._blackboard = blackboard;
            this._aggroModule = _blackboard.aggroModule;
            if (_aggroModule)
                _aggroModule.OnStateChanged += HandleStateChanged;

            reactions = GetComponents<IAggroReaction>();
            foreach (var reaction in reactions)
            {
                reaction.Initialize(_blackboard);
            }

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
        }
        
    }
}