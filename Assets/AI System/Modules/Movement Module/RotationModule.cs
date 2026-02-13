using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Sirenix.OdinInspector;

namespace Artifika.AI
{
    public enum RotationMode
    {
        /// <summary> Yaw follows NavMeshAgent.desiredVelocity (wandering, chasing, fleeing). </summary>
        Agent,
        /// <summary> Yaw faces the assigned look-at target (attacking with lookAt, aiming). </summary>
        LookAt,
        /// <summary> Yaw is frozen at its current value. </summary>
        None
    }

    /// <summary>
    /// Single authority for NPC yaw rotation.
    /// Resolves conflicts between NavMeshAgent, GroundFitter, and manual look-at
    /// by keeping agent.updateRotation = false at all times and feeding an absolute
    /// yaw angle into AIGroundFitter (or directly to the transform as fallback).
    /// </summary>
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

        public RotationMode CurrentMode => currentMode;
        public float CurrentYaw => currentYaw;
        
        public void Initialize(NavMeshAgent agent, AIGroundFitter fitter)
        {
            this.agent = agent;
            this.fitter = fitter;

            // NavMeshAgent must never touch rotation — we handle it.
            agent.updateRotation = false;
            agent.updateUpAxis  = false;

            currentYaw = transform.eulerAngles.y;
            targetYaw = currentYaw;
            activeRotationSpeed = defaultRotationSpeed;
            currentMode = RotationMode.Agent;

            if (fitter != null)
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
            lookTarget = target;
        }

        /// <summary> Clears the look-at target. </summary>
        public void ClearLookTarget()
        {
            lookTarget = null;
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
            Vector3 velocity = agent.desiredVelocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.01f)
                targetYaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            // else: keep previous targetYaw — NPC holds facing when stopped
        }

        private void ComputeLookAtYaw()
        {
            if (lookTarget == null) return;

            Vector3 direction = lookTarget.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
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
            if (fitter != null)
            {
                fitter.ExternalYaw = currentYaw;
                // Prevent base class from adding extra yaw through UpAxisRotation
                fitter.UpAxisRotation = 0f;
            }
            else
            {
                // Fallback for NPCs without ground fitting (flat terrain)
                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            }
        }
    }
}