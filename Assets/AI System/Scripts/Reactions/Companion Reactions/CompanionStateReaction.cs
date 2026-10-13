using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    [Title("Companion State Coordinator Reaction")]
    public sealed class CompanionStateReaction : SerializedMonoBehaviour, ICompanionReaction
    {
        private EntityCoordinator coordinator;

        public Dictionary<CompanionState, List<Behaviour>> disabledBehaviorsPerState = new Dictionary<CompanionState, List<Behaviour>>();

        public void Initialize(Blackboard blackboard)
        {
            coordinator = blackboard.GetComponent<EntityCoordinator>();
        }

        public void OnCompanionStateChanged(CompanionState previousState, CompanionState nextState)
        {
            if (!coordinator) return;

            if (disabledBehaviorsPerState.TryGetValue(previousState, out var behaviorsToEnable))
            {
                foreach (var behavior in behaviorsToEnable)
                {
                    if (behavior) behavior.enabled = true;
                }
            }

            if (disabledBehaviorsPerState.TryGetValue(nextState, out var behaviorsToDisable))
            {
                foreach (var behavior in behaviorsToDisable)
                {
                    if (behavior) behavior.enabled = false;
                }
            }

            switch (nextState)
            {
                case CompanionState.Mounted:
                case CompanionState.Disabled:
                    coordinator.DisableUpdate();
                    break;

                case CompanionState.Autonomous:
                case CompanionState.Commanded:
                    coordinator.EnableUpdate();
                    break;
            }
        }
    }
}
