using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
[RequireComponent(typeof(CommandModule))]
public class BehaviorCommandLinker : MonoBehaviour
{
    private BehaviorGraphAgent agent;
    private CommandModule commandModule;

    private const string AutonomousCommandKey = "Autonomous Command";
    private const string ActionCommandKey = "Action Command";

    private void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
        commandModule = GetComponent<CommandModule>();
    }

    private void OnEnable()
    {
        commandModule.OnAutonomousCommandChanged += HandleAutonomousChanged;
        commandModule.OnActionCommandChanged += HandleActionChanged;
        
        HandleAutonomousChanged(null, commandModule.CurrentAutonomousCommandDefinition);
        HandleActionChanged(null, commandModule.CurrentActionCommandDefinition);
    }

    private void OnDisable()
    {
        if (commandModule != null)
        {
            commandModule.OnAutonomousCommandChanged -= HandleAutonomousChanged;
            commandModule.OnActionCommandChanged -= HandleActionChanged;
        }
    }
    

    private void HandleAutonomousChanged(AutonomousCommandDefinition prev, AutonomousCommandDefinition next)
    {
        if (agent != null && agent.BlackboardReference != null)
        {
            agent.BlackboardReference.SetVariableValue(AutonomousCommandKey, next);
        }
    }

    private void HandleActionChanged(ActionCommandDefinition prev, ActionCommandDefinition next)
    {
        if (agent != null && agent.BlackboardReference != null)
        {
            agent.BlackboardReference.SetVariableValue(ActionCommandKey, next);
        }
    }
}