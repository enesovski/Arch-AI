using Archura.Attack;
using Archura.Death.Reactions;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public class HandleRootMotionReaction : MonoBehaviour, IDeathReaction, IDeathReactionInitializable
    {
        [Tooltip("Optional component implementing " + nameof(IGroundFitter) + ". Defaults to the Blackboard's fitter.")]
        [FormerlySerializedAs("groundFitter")]
        [SerializeField] private MonoBehaviour groundFitterSource;
        [SerializeField] private Animator animator;
        [SerializeField] private MovementModule movementModule;
        [SerializeField] private AttackModule attackModule;

        private IGroundFitter groundFitter;

        public void Initialize(Blackboard blackboard)
        {
            if (blackboard == null)
                return;

            groundFitter = groundFitterSource as IGroundFitter ?? blackboard.Fitter;
            animator = animator != null ? animator : blackboard.animator;
            movementModule = movementModule != null ? movementModule : blackboard.movementModule;
            attackModule = attackModule != null ? attackModule : blackboard.attackModule;
        }

        public void Execute()
        {
            if (attackModule)
                attackModule.InterruptAttackForDeath();

            if (movementModule)
            {
                movementModule.Stop();
                movementModule.SetStopped(true);
            }

            if (!UnityNull.IsNull(groundFitter))
                groundFitter.HandleRootMotion = false;

            if (animator != null)
                animator.applyRootMotion = false;
        }
    }
}
