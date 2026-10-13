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
    [NodeDescription(name: "ChaseTarget", story: "[Blackboard] chases [Target]", category: "Action", id: "4a1c3c1a2bd18e861799e0cd868ae237")]
    public partial class ChaseTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        private MovementModule movementModule;
        private AttackModule attackModule;

        private const float MOVE_THRESHOLD_SQR = 0.01f;
        private Vector3 lastDestination = Vector3.positiveInfinity;

        protected override Status OnStart()
        {
            var blackboard = Blackboard != null ? Blackboard.Value : null;
            movementModule = blackboard != null ? blackboard.movementModule : null;
            attackModule = blackboard != null ? blackboard.attackModule : null;
            lastDestination = Vector3.positiveInfinity;

            if (movementModule)
                movementModule.SetSpeed(movementModule.MovementProfile.runMovementSpeed);

            movementModule?.BeginMeleeMovement();

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            GameEntity targetEntity = Target.Value;

            if (!targetEntity)
                return Status.Success;

            if (!movementModule || !attackModule)
                return Status.Failure;

            if (attackModule.ShouldUseAttackBranch())
            {
                movementModule.Stop();
                return Status.Success;
            }

            if (movementModule.UpdateRangedMovement(targetEntity) || movementModule.UpdateMeleeMovement(targetEntity))
                return Status.Running;

            Transform threatPos = targetEntity.transform;
            if (!threatPos)
                return Status.Failure;

            Vector3 desiredPoint;

            if (MovementPointPicker.TryPickChasePoint(movementModule.transform, threatPos,
                    out Vector3 chasePoint))
            {
                desiredPoint = chasePoint;
            }
            else
            {
                desiredPoint = threatPos.position;
            }

            if ((lastDestination - desiredPoint).sqrMagnitude > MOVE_THRESHOLD_SQR)
            {
                movementModule.MoveTo(desiredPoint);
                lastDestination = desiredPoint;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            movementModule?.EndMeleeMovement();
            movementModule?.EndRangedMovement();
            lastDestination = Vector3.positiveInfinity;
            movementModule?.Stop();
        }
    }
}
