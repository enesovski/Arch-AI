using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable]
    public class SetCompanionCommandZoneBehavior : IZoneBehavior
    {
        public enum CommandType
        {
            Autonomous,
            Action
        }

        [Title("Command Settings")]
        [SerializeField]
        private CommandType commandType = CommandType.Autonomous;

        [SerializeField, Required]
        private string commandKey;

        [Title("Exit Settings")]
        [SerializeField, Tooltip("If true, Autonomous commands revert to their previous state, and Action commands are cleared on exit.")]
        private bool revertOnExit = true;

        public void OnEnter(GameObject other)
        {
            var commandModule = other.GetComponentInParent<CommandModule>();
            if (commandModule == null) return;

            if (commandType == CommandType.Autonomous)
            {
                commandModule.SetAutonomousCommand(commandKey);
            }
            else if (commandType == CommandType.Action)
            {
                commandModule.SetActionCommand(commandKey);
            }
        }

        public void OnExit(GameObject other)
        {
            if (!revertOnExit) return;

            var commandModule = other.GetComponentInParent<CommandModule>();
            if (commandModule == null) return;

            if (commandType == CommandType.Autonomous)
            {
                commandModule.RestorePreviousAutonomousCommand();
            }
            else if (commandType == CommandType.Action)
            {
                commandModule.ClearActionCommand();
            }
        }
    }
}
