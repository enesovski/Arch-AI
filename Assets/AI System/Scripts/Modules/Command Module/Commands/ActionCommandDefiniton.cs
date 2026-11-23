using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "Action Command Definition", menuName = "AI/Commands/Action Command Definition")]
    public class ActionCommandDefinition : CommandDefinitionBase
    {
        [Title("Action Command")]
        public BehaviorGraph subgraph;
    }
}
