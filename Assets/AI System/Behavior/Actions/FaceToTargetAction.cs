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
    [NodeDescription(
        name: "Face Target",
        story: "[Blackboard] faces [Target]",
        category: "Action",
        id: "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6")]
    public partial class FaceTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard>  Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity>  Target;

        private RotationModule rotationModule;
        private Blackboard blackboard;

        protected override Status OnStart()
        {
            blackboard = Blackboard != null ? Blackboard.Value : null;
            rotationModule = blackboard != null ? blackboard.rotationModule : null;

            if (rotationModule == null)
                return Status.Failure;

            if (!Target.Value)
                return Status.Success;

            rotationModule.SetLookTarget(Target.Value.transform);
            rotationModule.SetMode(RotationMode.LookAt);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (!Target.Value)
                return Status.Success;

            if (ShouldLeaveForAttack())
                return Status.Success;

            if (blackboard && blackboard.movementModule &&
                blackboard.movementModule.UpdateRangedMovement(Target.Value))
                return Status.Running;

            return Status.Running;
        }

        protected override void OnEnd()
        {
            blackboard?.movementModule?.EndRangedMovement();
            if (rotationModule == null) return;

            rotationModule.ClearLookTarget();
            rotationModule.SetMode(RotationMode.Agent);
        }

        private bool ShouldLeaveForAttack()
        {
            if (blackboard == null || blackboard.gameEntity == null)
                return false;

            if (blackboard.gameEntity.EntityType != EntityType.Aggressive && blackboard.gameEntity.EntityType != EntityType.Boss)
                return false;

            return blackboard.attackModule != null && blackboard.attackModule.ShouldUseAttackBranch();
        }
    }
}
