using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public abstract class CommandDefinitionBase : ScriptableObject
    {
        [Title("Command Definiton")]
        public string CommandKey => commandKey;
        public string DisplayName => displayName;

        [SerializeField] private string commandKey;
        [SerializeField] private string displayName;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(commandKey);
        }
    }
}
