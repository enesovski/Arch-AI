using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Behavior;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class CommandModule : NetworkBehaviour
{
    public string PreviousAutonomousCommand => previousAutonomousCommand;

    public AutonomousCommandDefinition CurrentAutonomousCommandDefinition =>
        GetAutonomousCommandOrNull(CurrentAutonomousCommand);
    public AutonomousCommandDefinition PreviousAutonomousCommandDefinition =>
        GetAutonomousCommandOrNull(previousAutonomousCommand);
    public ActionCommandDefinition CurrentActionCommandDefinition =>
        GetActionCommandOrNull(CurrentActionCommand);

    public event Action<AutonomousCommandDefinition, AutonomousCommandDefinition> OnAutonomousCommandChanged;
    public event Action<ActionCommandDefinition, ActionCommandDefinition> OnActionCommandChanged;

    [Title("Defaults")]
    [SerializeField] private string defaultAutonomousCommand = CompanionCommandKeys.FollowOwner;

    [Title("Autonomous Commands")]
    [SerializeField] private List<AutonomousCommandDefinition> autonomousCommands = new();

    [Title("Action Commands")]
    [SerializeField] private List<ActionCommandDefinition> actionCommands = new();

    [Title("Runtime Debug")]
    [HideInEditorMode]
    [ShowInInspector, DisplayAsString]
    public string CurrentAutonomousCommand => currentAutonomousCommand.Value.ToString();

    [HideInEditorMode]
    [ShowInInspector, DisplayAsString]
    public string CurrentActionCommand => currentActionCommand.Value.ToString();
    
    
    private readonly NetworkVariable<FixedString64Bytes> currentAutonomousCommand = new(
        new FixedString64Bytes(CompanionCommandKeys.None),
        writePerm: NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<FixedString64Bytes> currentActionCommand = new(
        new FixedString64Bytes(CompanionCommandKeys.None),
        writePerm: NetworkVariableWritePermission.Server);

    private string previousAutonomousCommand = CompanionCommandKeys.None;

    private readonly Dictionary<string, AutonomousCommandDefinition> autonomousLookup = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ActionCommandDefinition> actionLookup = new(StringComparer.Ordinal);

    private void Awake() => RebuildLookups();

#if UNITY_EDITOR
    private void OnValidate() => RebuildLookups();
#endif

    public override void OnNetworkSpawn()
    {
        RebuildLookups();

        currentAutonomousCommand.OnValueChanged += HandleAutonomousCommandNetworkChanged;
        currentActionCommand.OnValueChanged += HandleActionCommandNetworkChanged;

        if (IsServer && string.IsNullOrWhiteSpace(CurrentAutonomousCommand))
            SetAutonomousCommand(defaultAutonomousCommand);
    }

    public override void OnNetworkDespawn()
    {
        currentAutonomousCommand.OnValueChanged -= HandleAutonomousCommandNetworkChanged;
        currentActionCommand.OnValueChanged -= HandleActionCommandNetworkChanged;
    }

    // Fires on ALL clients when the server changes the autonomous command
    private void HandleAutonomousCommandNetworkChanged(FixedString64Bytes prev, FixedString64Bytes next)
    {
        var previousDef = GetAutonomousCommandOrNull(prev.ToString());
        var nextDef = GetAutonomousCommandOrNull(next.ToString());
        OnAutonomousCommandChanged?.Invoke(previousDef, nextDef);
    }

    // Fires on ALL clients when the server changes the action command
    private void HandleActionCommandNetworkChanged(FixedString64Bytes prev, FixedString64Bytes next)
    {
        var previousDef = GetActionCommandOrNull(prev.ToString());
        var nextDef = GetActionCommandOrNull(next.ToString());
        OnActionCommandChanged?.Invoke(previousDef, nextDef);
    }

    public void RebuildLookups()
    {
        autonomousLookup.Clear();
        actionLookup.Clear();

        foreach (var cmd in autonomousCommands)
            if (cmd != null && cmd.IsValid())
                autonomousLookup[cmd.CommandKey] = cmd;

        foreach (var cmd in actionCommands)
            if (cmd != null && cmd.IsValid())
                actionLookup[cmd.CommandKey] = cmd;
    }

    public bool SetAutonomousCommand(string commandKey)
    {
        if (!IsServer) return false;
        if (!TryGetAutonomousCommand(commandKey, out _)) return false;

        var current = CurrentAutonomousCommand;
        if (string.Equals(current, commandKey, StringComparison.Ordinal)) return true;

        previousAutonomousCommand = current;
        currentAutonomousCommand.Value = new FixedString64Bytes(commandKey);
        return true;
    }
    
    public void SetAutonomousCommand(AutonomousCommandDefinition commandDefinition)
    {
        SetAutonomousCommand(commandDefinition.CommandKey);
    }


    public bool SetActionCommand(string commandKey)
    {
        if (!IsServer) return false;
        if (!TryGetActionCommand(commandKey, out _)) return false;

        if (string.Equals(CurrentActionCommand, commandKey, StringComparison.Ordinal)) return true;

        currentActionCommand.Value = new FixedString64Bytes(commandKey);
        return true;
    }

    public void ClearActionCommand()
    {
        if (!IsServer) return;
        if (string.IsNullOrWhiteSpace(CurrentActionCommand)) return;
        currentActionCommand.Value = new FixedString64Bytes(CompanionCommandKeys.None);
    }

    public bool RestorePreviousAutonomousCommand()
    {
        if (!IsServer) return false;
        return string.IsNullOrWhiteSpace(previousAutonomousCommand)
            ? SetAutonomousCommand(defaultAutonomousCommand)
            : SetAutonomousCommand(previousAutonomousCommand);
    }

    public bool TryGetAutonomousCommand(string key, out AutonomousCommandDefinition def)
    {
        def = null;
        return !string.IsNullOrWhiteSpace(key) && autonomousLookup.TryGetValue(key, out def);
    }

    public bool TryGetActionCommand(string key, out ActionCommandDefinition def)
    {
        def = null;
        return !string.IsNullOrWhiteSpace(key) && actionLookup.TryGetValue(key, out def);
    }

    public bool HasAutonomousCommand(string key) => autonomousLookup.ContainsKey(key);
    public bool HasActionCommand(string key) => actionLookup.ContainsKey(key);

    private AutonomousCommandDefinition GetAutonomousCommandOrNull(string key)
    {
        TryGetAutonomousCommand(key, out var def); return def;
    }

    private ActionCommandDefinition GetActionCommandOrNull(string key)
    {
        TryGetActionCommand(key, out var def); return def;
    }
}