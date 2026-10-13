using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
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
}
