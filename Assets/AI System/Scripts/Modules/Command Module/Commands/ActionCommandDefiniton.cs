using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEngine;

[CreateAssetMenu(fileName = "Action Command Definition", menuName = "AI/Commands/Action Command Definition")]
public class ActionCommandDefinition : CommandDefinitionBase
{
    [Title("Action Command")]
    public BehaviorGraph subgraph;
}