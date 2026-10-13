using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public class AnimatorModule : MonoBehaviour
{
    public const string ATTACKING_PARAMETER = "IsAttacking";
    public const string ATTACKINDEX_PARAMETER = "AttackIndex";
    public const string SPEED_PARAMETER = "Speed";
    private int ATTACKING_HASH;
    private int ATTACKINDEX_HASH;
    private int SPEED_HASH;

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField, Min(0f)] private float speedDampTime = 0.08f;
    private MovementModule movementModule;

    private float minSpeed;
    private float maxSpeed;
    private float targetSpeedParam;
    
    private void Awake()
    {
        ATTACKING_HASH = Animator.StringToHash(ATTACKING_PARAMETER);
        ATTACKINDEX_HASH = Animator.StringToHash(ATTACKINDEX_PARAMETER);
        SPEED_HASH = Animator.StringToHash(SPEED_PARAMETER);

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
            animator.SetFloat(SPEED_HASH, targetSpeedParam);
    }

    private void Update()
    {
        if (!animator || speedDampTime <= 0f)
            return;

        animator.SetFloat(SPEED_HASH, targetSpeedParam, speedDampTime, Time.deltaTime);
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
        animator.SetInteger(ATTACKINDEX_HASH, index);
    }

    public void SetAttacking(bool attacking)
    {
        animator.SetBool(ATTACKING_HASH, attacking);
    }

    /// <summary>
    /// Multiplies the global playback speed of the Animator. 
    /// 1 = Normal speed, 0.5 = Slow motion (Freeze), 1.5 = Fast motion (Overclock)
    /// </summary>
    public void SetAnimationSpeedMultiplier(float multiplier)
    {
        if (animator)
        {
            animator.speed = multiplier;
        }
    }
}
