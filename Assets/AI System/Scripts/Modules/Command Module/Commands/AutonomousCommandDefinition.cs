using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "Autonomous Command Definition", menuName = "AI/Commands/Autonomous Command Definition")]
    public class AutonomousCommandDefinition: CommandDefinitionBase
    {
        [Title("Autonomous Command")]
        public BehaviorGraph subgraph;
    }
}
