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
    private MovementProfile movementData;

    private void Awake()
    {
        ATTACKING_HASH = Animator.StringToHash(ATTACKING_PARAMETER);
        ATTACKINDEX_HASH = Animator.StringToHash(ATTACKINDEX_PARAMETER);
        SPEED_HASH = Animator.StringToHash(SPEED_PARAMETER);

        movementData = GetComponent<MovementModule>().MovementProfile;
    }
    public void SetSpeedParam(float desiredSpeed)
    {
        if (!animator || movementData == null)
            return;

        animator.SetFloat(SPEED_HASH, MapDesiredSpeedToAnim(desiredSpeed, movementData));
    }

    public static float MapDesiredSpeedToAnim(float speed, MovementProfile data)
    {
        if (speed <= 0.0001f) return 0f;

        float min = Mathf.Max(0.0001f, data.MinPossibleSpeed);
        float max = Mathf.Max(min + 0.0001f, data.MaxPossibleSpeed);

        if (speed <= min)
        {
            return Mathf.Lerp(0f, 1f, speed / min);
        }

        float t = Mathf.InverseLerp(min, max, speed);
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

}
