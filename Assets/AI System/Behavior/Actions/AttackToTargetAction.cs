using System;
using Artifika.AI.Attack;
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
    [NodeDescription(name: "AttackToTarget", story: "[Blackboard] attacks to Target", category: "AI", id: "12f0e122f0a182b5eb420fad6d5802fe")]
    public partial class AttackToTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        private AttackModule attackModule;
        private bool attackStarted;

        protected override Status OnStart()
        {
            var blackboard = Blackboard != null ? Blackboard.Value : null;
            attackModule = blackboard != null ? blackboard.attackModule : null;
            attackStarted = false;

            if (!attackModule)
                return Status.Failure;

            if (attackModule.IsAttackCommitted)
            {
                attackStarted = true;
                return Status.Running;
            }

            if (!attackModule.CanStartAttack())
                return Status.Failure;

            if (!attackModule.Attack())
                return Status.Failure;

            attackStarted = true;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (!attackModule)
                return Status.Failure;

            if (!attackStarted)
                return Status.Failure;

            if (!attackModule.IsAttackCommitted)
                return Status.Success;

            return Status.Running;
        }

        protected override void OnEnd()
        {
            attackStarted = false;
        }
    }
}
