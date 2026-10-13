using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public class CompanionBehaviorAssigner : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private Blackboard blackboard;

        [Title("Behavior Variable Names")]
        [SerializeField] private string companionStatusVar = "Companion Status";
        [SerializeField] private string autonomousCommandVar = "Autonomous Command";
        [SerializeField] private string actionCommandVar = "Action Command";

        private CompanionStateModule StateModule => blackboard != null ? blackboard.companionStateModule : null;
        private CommandModule CommandModule => blackboard != null ? blackboard.commandModule : null;

        private BlackboardReference blackboardReference;

        private void OnEnable()
        {
            blackboardReference = blackboard.behaviorGraphAgent.BlackboardReference;
            Subscribe();

            SyncCurrentValues();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void SyncCurrentValues()
        {
            if (StateModule != null)
                blackboardReference.SetVariableValue(companionStatusVar, ToBehaviorStatus(StateModule.CurrentState));

            if (CommandModule != null)
            {
                blackboardReference.SetVariableValue(autonomousCommandVar,
                    CommandModule.CurrentAutonomousCommandDefinition);
                blackboardReference.SetVariableValue(actionCommandVar, CommandModule.CurrentActionCommandDefinition);
            }
        }

        private CompanionStatus ToBehaviorStatus(CompanionState state)
        {
            return state switch
            {
                CompanionState.Autonomous => CompanionStatus.Autonomous,
                CompanionState.Commanded => CompanionStatus.Commanded,
                CompanionState.Mounted => CompanionStatus.Mounted,
                CompanionState.Disabled => CompanionStatus.Disabled,
                _ => CompanionStatus.Disabled
            };
        }

        #region Event Handling

        private void Subscribe()
        {
            if (StateModule != null)
                StateModule.OnStateChanged += HandleStateChanged;

            if (CommandModule != null)
            {
                CommandModule.OnAutonomousCommandChanged += HandleAutonomousCommandChanged;
                CommandModule.OnActionCommandChanged += HandleActionCommandChanged;
            }
        }

        private void Unsubscribe()
        {
            if (StateModule != null)
                StateModule.OnStateChanged -= HandleStateChanged;

            if (CommandModule != null)
            {
                CommandModule.OnAutonomousCommandChanged -= HandleAutonomousCommandChanged;
                CommandModule.OnActionCommandChanged -= HandleActionCommandChanged;
            }
        }

        private void HandleStateChanged(CompanionState previousState, CompanionState nextState)
        {
            blackboardReference.SetVariableValue(companionStatusVar, (int)nextState);
        }

        private void HandleAutonomousCommandChanged(
            AutonomousCommandDefinition previousCommand, AutonomousCommandDefinition nextCommand)
        {
            blackboardReference.SetVariableValue(autonomousCommandVar, nextCommand);
        }

        private void HandleActionCommandChanged(
            ActionCommandDefinition previousCommand, ActionCommandDefinition nextCommand)
        {
            blackboardReference.SetVariableValue(actionCommandVar, nextCommand);
        }

        #endregion
    }
}
