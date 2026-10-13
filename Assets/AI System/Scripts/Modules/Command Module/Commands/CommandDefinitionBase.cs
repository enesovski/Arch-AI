using Sirenix.OdinInspector;
using UnityEngine;

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
