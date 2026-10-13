using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class SetCompanionStateZoneBehavior : IZoneBehavior
{
    [Title("Enter Settings")]
    [SerializeField]
    private CompanionState stateOnEnter = CompanionState.Disabled;
    
    public void OnEnter(GameObject other)
    {
        var stateModule = other.GetComponentInParent<CompanionStateModule>();
        if (stateModule != null)
        {
            stateModule.SetState(stateOnEnter);
        }
    }

    public void OnExit(GameObject other)
    {

    }
}