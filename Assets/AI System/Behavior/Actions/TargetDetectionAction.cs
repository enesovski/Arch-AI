using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "TargetDetection", story: "[AggroModule] sets [AggroStatus] and [Target]", category: "Action", id: "65e8b136de28e7593d55e7e0e788791f")]
    public partial class TargetDetectionAction : Action
    {
        [SerializeReference] public BlackboardVariable<AggroModule> AggroModule;
        [SerializeReference] public BlackboardVariable<AggroStatus> AggroStatus;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;
        protected override Status OnStart()
        {
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            AggroModule aggroModule = AggroModule.Value;
            if (aggroModule == null || AggroStatus == null)
            {
                return Status.Failure;
            }

            switch(aggroModule.CurrentAggroState)
            {
                case AggroState.Passive:
                    Target.Value = null;
                    AggroStatus.Value = global::Artifika.AI.AggroStatus.Passive;
                    break;

                case AggroState.Suspicious:
                    Target.Value = null;
                    AggroStatus.Value = global::Artifika.AI.AggroStatus.Suspicious;
                    break;

                case AggroState.Alerted:
                    Target.Value = aggroModule.CurrentTarget;
                    AggroStatus.Value = global::Artifika.AI.AggroStatus.Aggressive;
                    break;
            }

            return Status.Success;
        }

        protected override void OnEnd()
        {
        }
    }
}
