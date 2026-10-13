using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SetActionCommand", story: "[Blackboard] selects [Subgraph] from [ActionCommand]", category: "Action", id: "4468dc1da290b1b98434bd209cab5ffd")]
    public partial class SetActionCommandAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<ActionCommandDefinition> ActionCommand;
        [SerializeReference] public BlackboardVariable<BehaviorGraph> Subgraph;

        protected override Status OnStart()
        {
            var bb  = Blackboard?.Value;
            var cmd = ActionCommand?.Value;

            if (bb == null || cmd?.subgraph == null)
                return Status.Failure;

            Subgraph.Value = cmd.subgraph;
            return Status.Success;
        }
    }
}
