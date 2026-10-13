using Artifika.AI.Death.Reactions;
using Artifika.AI;
using Artifika.AI.Attack;
using UnityEngine;

public class HandleRootMotionReaction : MonoBehaviour, IDeathReaction, IDeathReactionInitializable
{
    [SerializeField] private AIGroundFitter groundFitter;
    [SerializeField] private Animator animator;
    [SerializeField] private MovementModule movementModule;
    [SerializeField] private AttackModule attackModule;

    public void Initialize(Blackboard blackboard)
    {
        if (blackboard == null)
            return;

        groundFitter = groundFitter != null ? groundFitter : blackboard.fitter;
        animator = animator != null ? animator : blackboard.animator;
        movementModule = movementModule != null ? movementModule : blackboard.movementModule;
        attackModule = attackModule != null ? attackModule : blackboard.attackModule;
    }
    
    public void Execute()
    {
        attackModule?.InterruptAttackForDeath();
        movementModule?.Stop();
        movementModule?.SetStopped(true);

        if (groundFitter != null)
            groundFitter.HandleRootMotion = false;

        if (animator != null)
            animator.applyRootMotion = false;
    }
}
