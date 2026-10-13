using System;
using Artifika.AI.Aggro;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using Action = Unity.Behavior.Action;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SearchForTarget", story: "[Blackboard] searches area", category: "Action", id: "a3bd44c8dee8aab5311894dcf77d8888")]
    public partial class SearchForTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

        private MovementModule movementModule;
        private AggroModule aggroModule;
        private RotationModule rotationModule;

        private float minSearchDistance = 3f;
        private float maxSearchDistance = 6f;
        private float waitAtPointDuration = 2f;

        private enum SearchPhase
        {
            GoingToLastSeen,
            MovingToSearchPoint,
            WaitingAtSearchPoint
        }

        private SearchPhase currentPhase;
        private Vector3 searchCenter;
        private float waitUntilTime;

        protected override Status OnStart()
        {
            if (Blackboard?.Value == null)
                return Status.Failure;

            movementModule = Blackboard.Value.movementModule;
            aggroModule = Blackboard.Value.aggroModule;
            rotationModule = Blackboard.Value.rotationModule;

            if (movementModule == null || aggroModule == null || movementModule.MovementProfile == null)
                return Status.Failure;

            minSearchDistance =  movementModule.MovementProfile.MinSearchDistance;
            maxSearchDistance =  movementModule.MovementProfile.MaxSearchDistance;
            waitAtPointDuration = Mathf.Max(0f, movementModule.MovementProfile.lookAroundDuration);

            currentPhase = SearchPhase.GoingToLastSeen;
            waitUntilTime = 0f;
            searchCenter = aggroModule.SearchOrigin;

            movementModule.SetSpeed(movementModule.MovementProfile.searchMovementSpeed);
            rotationModule?.SetMode(RotationMode.Agent);

            if (!MoveToSearchPoint(searchCenter))
                MoveToNextSearchPoint();

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (aggroModule.CurrentSuspiciousState != SuspiciousState.Searching)
                return Status.Success;

            switch (currentPhase)
            {
                case SearchPhase.GoingToLastSeen:
                    return UpdateGoingToLastSeen();

                case SearchPhase.MovingToSearchPoint:
                    return UpdateMovingToSearchPoint();

                case SearchPhase.WaitingAtSearchPoint:
                    return UpdateWaitingAtSearchPoint();

                default:
                    return Status.Failure;
            }
        }

        private Status UpdateGoingToLastSeen()
        {
            if (movementModule.HasArrived())
            {
                StartWaitingAtCurrentPoint();
            }

            return Status.Running;
        }

        private Status UpdateMovingToSearchPoint()
        {
            if (movementModule.HasArrived())
            {
                StartWaitingAtCurrentPoint();
            }

            return Status.Running;
        }

        private Status UpdateWaitingAtSearchPoint()
        {
            if (Time.time >= waitUntilTime)
                MoveToNextSearchPoint();

            return Status.Running;
        }

        private void StartWaitingAtCurrentPoint()
        {
            movementModule.Stop();
            waitUntilTime = Time.time + waitAtPointDuration;
            currentPhase = SearchPhase.WaitingAtSearchPoint;
        }

        private void MoveToNextSearchPoint()
        {
            if (TryPickSearchPoint(searchCenter, out Vector3 point) && MoveToSearchPoint(point))
            {
                currentPhase = SearchPhase.MovingToSearchPoint;
                return;
            }

            StartWaitingAtCurrentPoint();
        }

        private bool TryPickSearchPoint(Vector3 center, out Vector3 point)
        {
            const int attempts = 12;

            for (int i = 0; i < attempts; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f);
                float distance = UnityEngine.Random.Range(minSearchDistance, maxSearchDistance);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;

                if (TryResolveSearchPoint(center + offset, out point))
                    return true;
            }

            point = center;
            return false;
        }

        private bool MoveToSearchPoint(Vector3 point)
        {
            if (!TryResolveSearchPoint(point, out Vector3 validPoint))
                return false;

            movementModule.SetSpeed(movementModule.MovementProfile.searchMovementSpeed);
            return movementModule.MoveTo(validPoint);
        }

        private bool TryResolveSearchPoint(Vector3 point, out Vector3 validPoint)
        {
            validPoint = point;

            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                return false;

            if (!MovementPointPicker.IsPathReachable(movementModule.transform.position, hit.position))
                return false;

            validPoint = hit.position;
            return true;
        }

        protected override void OnEnd()
        {
            movementModule?.Stop();
            rotationModule?.SetMode(RotationMode.Agent);
        }
    }
}
