using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class AnimatorModule : MonoBehaviour
    {
        public const string ATTACKING_PARAMETER = "IsAttacking";
        public const string ATTACKINDEX_PARAMETER = "AttackIndex";
        public const string SPEED_PARAMETER = "Speed";
        private int attackingHash;
        private int attackindexHash;
        private int speedHash;

        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField, Min(0f)] private float speedDampTime = 0.08f;
        private MovementModule movementModule;

        private float minSpeed;
        private float maxSpeed;
        private float targetSpeedParam;

        private void Awake()
        {
            attackingHash = Animator.StringToHash(ATTACKING_PARAMETER);
            attackindexHash = Animator.StringToHash(ATTACKINDEX_PARAMETER);
            speedHash = Animator.StringToHash(SPEED_PARAMETER);

            movementModule = GetComponent<MovementModule>();
            if (movementModule != null)
            {
                minSpeed = movementModule.CalculateMinSpeed();
                maxSpeed = movementModule.CalculateMaxSpeed();
            }
        }
        public void SetSpeedParam(float desiredSpeed)
        {
            if (!animator || !movementModule)
                return;

            targetSpeedParam = MapDesiredSpeedToAnim(desiredSpeed);

            if (targetSpeedParam <= 0.0001f || speedDampTime <= 0f)
                animator.SetFloat(speedHash, targetSpeedParam);
        }

        private void Update()
        {
            if (!animator || speedDampTime <= 0f)
                return;

            animator.SetFloat(speedHash, targetSpeedParam, speedDampTime, Time.deltaTime);
        }

        private float MapDesiredSpeedToAnim(float currSpeed)
        {
            if (currSpeed <= 0.0001f) return 0f;

            if (currSpeed <= minSpeed)
            {
                return Mathf.Lerp(0f, 1f, currSpeed / minSpeed);
            }

            float t = Mathf.InverseLerp(minSpeed, maxSpeed, currSpeed);
            return Mathf.Lerp(1f, 2f, t);
        }

        public void SetAttackIndex(int index)
        {
            animator.SetInteger(attackindexHash, index);
        }

        public void SetAttacking(bool attacking)
        {
            animator.SetBool(attackingHash, attacking);
        }

        public void SetAnimationSpeedMultiplier(float multiplier)
        {
            if (animator)
            {
                animator.speed = multiplier;
            }
        }
    }
}
