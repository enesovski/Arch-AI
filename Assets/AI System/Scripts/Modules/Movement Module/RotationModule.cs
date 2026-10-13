using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Sirenix.OdinInspector;

namespace Artifika.AI
{
    public enum RotationMode
    {
        Agent,
        LookAt,
        None
    }
    [DisallowMultipleComponent]
    public class RotationModule : NetworkBehaviour
    {
        [Title("Configuration")]
        [SerializeField] private float defaultRotationSpeed = 360f;
        
        [Title("Debug")]
        [SerializeField, ReadOnly] private RotationMode currentMode;
        [SerializeField, ReadOnly] private float currentYaw;
        [SerializeField, ReadOnly] private float targetYaw;

        private NavMeshAgent agent;
        private AIGroundFitter fitter;
        
        private float activeRotationSpeed;
        private Transform lookTarget;
        private Vector3 lookTargetOffset;

        public RotationMode CurrentMode => currentMode;
        public float CurrentYaw => currentYaw;
        
        public void Initialize(NavMeshAgent agent, AIGroundFitter fitter)
        {
            this.agent = agent;
            this.fitter = fitter;

            agent.updateRotation = false;
            agent.updateUpAxis  = false;

            currentYaw = transform.eulerAngles.y;
            targetYaw = currentYaw;
            activeRotationSpeed = defaultRotationSpeed;
            currentMode = RotationMode.Agent;

            if (fitter)
            {
                fitter.UseExternalYaw = true;
                fitter.ExternalYaw    = currentYaw;
            }
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
        }

        #region Public API

        /// <summary> Sets the rotation strategy. Safe to call from BT nodes, reactions, or modules. </summary>
        public void SetMode(RotationMode mode)
        {
            currentMode = mode;
        }

        /// <summary> Assigns a world-space transform for LookAt mode to face. </summary>
        public void SetLookTarget(Transform target)
        {
            SetLookTarget(target, Vector3.zero);
        }

        public void SetLookTarget(Transform target, Vector3 localOffset)
        {
            lookTarget = target;
            lookTargetOffset = localOffset;
        }

        /// <summary> Clears the look-at target. </summary>
        public void ClearLookTarget()
        {
            lookTarget = null;
            lookTargetOffset = Vector3.zero;
        }

        /// <summary> Override rotation speed (e.g. slower aim turn, faster snap). </summary>
        public void SetRotationSpeed(float speed)
        {
            activeRotationSpeed = speed;
        }

        /// <summary> Restores rotation speed to the inspector default. </summary>
        public void ResetRotationSpeed()
        {
            activeRotationSpeed = defaultRotationSpeed;
        }

        /// <summary> Instantly snaps yaw to a world angle. Useful for spawning or teleporting. </summary>
        public void SnapYaw(float worldYaw)
        {
            currentYaw = worldYaw;
            targetYaw = worldYaw;
            ApplyYaw();
        }

        #endregion

        private void Update()
        {
            ComputeTargetYaw();
            StepYaw();
            ApplyYaw();
        }

        private void ComputeTargetYaw()
        {
            switch (currentMode)
            {
                case RotationMode.Agent:
                    ComputeAgentYaw();
                    break;

                case RotationMode.LookAt:
                    ComputeLookAtYaw();
                    break;

                case RotationMode.None:
                    targetYaw = currentYaw;
                    break;
            }
        }

        private void ComputeAgentYaw()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh ||
                agent.isStopped || agent.pathPending || !agent.hasPath ||
                agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.05f))
                return;

            // desiredVelocity includes local avoidance. In a crowd it can alternate
            // left and right while the agent barely moves, making the body twitch.
            // The next path corner is a stable facing direction in Agent mode.
            Vector3 pathDir = agent.steeringTarget - transform.position;
            pathDir.y = 0f;

            Vector3 actualVelocity = agent.velocity;
            actualVelocity.y = 0f;

            if (actualVelocity.sqrMagnitude > 0.01f && pathDir.sqrMagnitude > 0.01f)
            {
                targetYaw = Mathf.Atan2(pathDir.x, pathDir.z) * Mathf.Rad2Deg;
            }
        }

        private void ComputeLookAtYaw()
        {
            if (!lookTarget) return;

            Vector3 targetPosition = lookTarget.TransformPoint(lookTargetOffset);
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            // Increased threshold to avoid violent 180-degree flips when targets overlap
            if (direction.sqrMagnitude > 0.01f)
                targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private void StepYaw()
        {
            currentYaw = Mathf.MoveTowardsAngle(
                currentYaw,
                targetYaw,
                activeRotationSpeed * Time.deltaTime
            );
        }

        private void ApplyYaw()
        {
            if (fitter)
            {
                fitter.ExternalYaw = currentYaw;
                fitter.UpAxisRotation = 0f;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            }
        }
    }
}
