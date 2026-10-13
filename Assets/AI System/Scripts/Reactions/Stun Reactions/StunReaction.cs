using UnityEngine;
using Sirenix.OdinInspector;
using Artifika.AI.Attack;
using MoreMountains.Feedbacks;

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
        
        private MovementModule _movementModule;
        private RotationModule _rotationModule;
        private AttackModule _attackModule;
        private AnimatorModule _animatorModule;
        private Animator _animator;
        private EntityCoordinator _coordinator;


        private int _stunAnimHash;
        private RotationMode _preStunRotationMode = RotationMode.Agent;
        private Coroutine _releaseCoroutine;
        private bool _releasePending;

        public void Initialize(Blackboard blackboard)
        {
            _movementModule = blackboard.movementModule;
            _rotationModule = blackboard.rotationModule;
            _attackModule = blackboard.attackModule;
            _animatorModule = blackboard.animatorModule;
            _animator = blackboard.animator;
            _coordinator = blackboard.GetComponent<EntityCoordinator>();

            _stunAnimHash = string.IsNullOrWhiteSpace(stunAnimParam) ? 0 : Animator.StringToHash(stunAnimParam);
        }

        public void OnStunnedChanged(bool isStunned)
        {
            if (isStunned)
            {
                CancelPendingRelease();
                _attackModule?.InterruptAttack();
                _animatorModule?.SetSpeedParam(0f);

                // Stop and disable movement
                if (_movementModule != null)
                {
                    _movementModule.Stop();
                    _movementModule.SetStopped(true);
                }

                // Stop rotation
                if (_rotationModule != null)
                {
                    _preStunRotationMode = _rotationModule.CurrentMode;
                    _rotationModule.SetMode(RotationMode.None);
                }

                // Pause AI updates
                if (_coordinator != null)
                {
                    _coordinator.AddUpdateLock(this);
                }

                // Play animation
                if (_animator != null && _stunAnimHash != 0)
                {
                    _animator.SetBool(_stunAnimHash, true);
                    
                }

                if (stunFeedback != null) stunFeedback.PlayFeedbacks();
            }
            else
            {
                if (_animator != null && _stunAnimHash != 0)
                {
                    _animator.SetBool(_stunAnimHash, false);
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
            if (!_releasePending)
                return;

            CancelPendingRelease();
            ReleaseStunControl();
        }

        private void BeginPendingRelease()
        {
            _releasePending = true;

            if (maxOutAnimationWaitSeconds <= 0f)
                return;

            if (_releaseCoroutine != null)
                StopCoroutine(_releaseCoroutine);

            _releaseCoroutine = StartCoroutine(ReleaseAfterFallbackDelay());
        }

        private System.Collections.IEnumerator ReleaseAfterFallbackDelay()
        {
            yield return new WaitForSeconds(maxOutAnimationWaitSeconds);

            _releaseCoroutine = null;
            if (_releasePending)
            {
                _releasePending = false;
                ReleaseStunControl();
            }
        }

        private void CancelPendingRelease()
        {
            _releasePending = false;

            if (_releaseCoroutine == null)
                return;

            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        private void ReleaseStunControl()
        {
            if (_movementModule != null)
            {
                _movementModule.SetStopped(false);
            }

            if (_rotationModule != null)
            {
                _rotationModule.SetMode(_preStunRotationMode);
            }

            if (_coordinator != null)
            {
                _coordinator.RemoveUpdateLock(this);
            }
        }
    }
}
