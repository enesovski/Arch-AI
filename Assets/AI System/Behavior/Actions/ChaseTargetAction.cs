using System;
using Archura.Attack;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Archura.Blackboard;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ChaseTarget", story: "[Blackboard] chases [Target]", category: "Action", id: "4a1c3c1a2bd18e861799e0cd868ae237")]
    public partial class ChaseTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
        [SerializeReference] public BlackboardVariable<GameEntity> Target;

        private MovementModule movementModule;
        private AttackModule attackModule;

        private const float MOVE_THRESHOLD_SQR = 0.01f;
        private readonly DestinationThrottle destinationThrottle = new DestinationThrottle();

        protected override Status OnStart()
        {
            Blackboard blackboard = Blackboard != null ? Blackboard.Value : null;
            movementModule = blackboard ? blackboard.movementModule : null;
            attackModule = blackboard ? blackboard.attackModule : null;
            destinationThrottle.Reset();

            if (movementModule && movementModule.MovementProfile)
                movementModule.SetSpeed(movementModule.MovementProfile.runMovementSpeed);

            if (movementModule)
                movementModule.BeginMeleeMovement();

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

            if (destinationThrottle.ShouldMoveTo(desiredPoint, MOVE_THRESHOLD_SQR))
                movementModule.MoveTo(desiredPoint);

            return Status.Running;
        }

        protected override void OnEnd()
        {
            destinationThrottle.Reset();
            if (!movementModule)
                return;

            movementModule.EndMeleeMovement();
            movementModule.EndRangedMovement();
            movementModule.Stop();
        }
    }
}
