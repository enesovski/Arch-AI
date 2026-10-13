using Artifika.AI.Attack;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Stun.Reactions
{
    [DisallowMultipleComponent]
    [Title("Stun Reaction")]
    public sealed class StunReaction : MonoBehaviour, IStunReaction
    {
        [Title("Animation")]
        [SerializeField] private string stunAnimParam = "IsStunned";
        [SerializeField] private bool waitForOutAnimationExit = true;
        [SerializeField, Min(0f)] private float maxOutAnimationWaitSeconds = 2f;

        [Title("Feedback")]
        [SerializeField] private MMF_Player stunFeedback;

        private MovementModule movementModule;
        private RotationModule rotationModule;
        private AttackModule attackModule;
        private AnimatorModule animatorModule;
        private Animator animator;
        private EntityCoordinator coordinator;

        private int stunAnimHash;
        private RotationMode preStunRotationMode = RotationMode.Agent;
        private Coroutine releaseCoroutine;
        private bool releasePending;

        public void Initialize(Blackboard blackboard)
        {
            movementModule = blackboard.movementModule;
            rotationModule = blackboard.rotationModule;
            attackModule = blackboard.attackModule;
            animatorModule = blackboard.animatorModule;
            animator = blackboard.animator;
            coordinator = blackboard.GetComponent<EntityCoordinator>();

            stunAnimHash = string.IsNullOrWhiteSpace(stunAnimParam) ? 0 : Animator.StringToHash(stunAnimParam);
        }

        public void OnStunnedChanged(bool isStunned)
        {
            if (isStunned)
            {
                CancelPendingRelease();
                attackModule?.InterruptAttack();
                animatorModule?.SetSpeedParam(0f);

                if (movementModule != null)
                {
                    movementModule.Stop();
                    movementModule.SetStopped(true);
                }

                if (rotationModule != null)
                {
                    preStunRotationMode = rotationModule.CurrentMode;
                    rotationModule.SetMode(RotationMode.None);
                }

                if (coordinator != null)
                {
                    coordinator.AddUpdateLock(this);
                }

                if (animator != null && stunAnimHash != 0)
                {
                    animator.SetBool(stunAnimHash, true);
                }

                if (stunFeedback != null) stunFeedback.PlayFeedbacks();
            }
            else
            {
                if (animator != null && stunAnimHash != 0)
                {
                    animator.SetBool(stunAnimHash, false);
                }

                if (waitForOutAnimationExit)
                {
                    BeginPendingRelease();
                }
                else
                {
                    ReleaseStunControl();
                }
            }
        }

        public void NotifyStunOutAnimationFinished()
        {
            if (!releasePending)
                return;

            CancelPendingRelease();
            ReleaseStunControl();
        }

        private void BeginPendingRelease()
        {
            releasePending = true;

            if (maxOutAnimationWaitSeconds <= 0f)
                return;

            if (releaseCoroutine != null)
                StopCoroutine(releaseCoroutine);

            releaseCoroutine = StartCoroutine(ReleaseAfterFallbackDelay());
        }

        private System.Collections.IEnumerator ReleaseAfterFallbackDelay()
        {
            yield return new WaitForSeconds(maxOutAnimationWaitSeconds);

            releaseCoroutine = null;
            if (releasePending)
            {
                releasePending = false;
                ReleaseStunControl();
            }
        }

        private void CancelPendingRelease()
        {
            releasePending = false;

            if (releaseCoroutine == null)
                return;

            StopCoroutine(releaseCoroutine);
            releaseCoroutine = null;
        }

        private void ReleaseStunControl()
        {
            if (movementModule != null)
            {
                movementModule.SetStopped(false);
            }

            if (rotationModule != null)
            {
                rotationModule.SetMode(preStunRotationMode);
            }

            if (coordinator != null)
            {
                coordinator.RemoveUpdateLock(this);
            }
        }
    }
}
