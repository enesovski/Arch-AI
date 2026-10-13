using System;
using UnityEngine;
using Artifika.AI;

[DisallowMultipleComponent]
public class CompanionStateReactionsBus : ReactionBusBase
{
    private CompanionStateModule _companionStateModule;
    private Blackboard _blackboard;
        
    private ICompanionReaction[] reactions;

    public void Initialize(Blackboard blackboard)
    {
        this._blackboard = blackboard;
        
        this._companionStateModule = _blackboard.companionStateModule; 
        
        if (_companionStateModule)
        {
            _companionStateModule.OnStateChanged += HandleStateChanged;
        }

        reactions = GetComponents<ICompanionReaction>();
        foreach (var reaction in reactions)
        {
            reaction.Initialize(_blackboard);
        }
    }
        
    private void OnDisable()
    {
        if (_companionStateModule != null)
        {
            _companionStateModule.OnStateChanged -= HandleStateChanged;
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