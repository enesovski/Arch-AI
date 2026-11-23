using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "Autonomous Command Definition", menuName = "AI/Commands/Autonomous Command Definition")]
    public class AutonomousCommandDefinition: CommandDefinitionBase
    {
        [Title("Autonomous Command")]
        public BehaviorGraph subgraph;
    }
}
