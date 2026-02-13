using UnityEngine;
using Sirenix.OdinInspector;

namespace Artifika.AI.Aggro.Reactions
{
    [DisallowMultipleComponent]
    [Title("Aggro Animator Reaction")]
    public sealed class AggroAnimatorReaction : MonoBehaviour, IAggroReaction
    {
        public enum ParamMode { Bool, Trigger }

        private Animator animator;
        private AnimatorModule animatorModule;

        [FoldoutGroup("Suspicious"), SerializeField]
        private string suspiciousParam = "Suspicious";

        [FoldoutGroup("Suspicious"), SerializeField, LabelText("Param Type")]
        private ParamMode suspiciousMode = ParamMode.Bool;

        [FoldoutGroup("Alerted"), SerializeField]
        private string alertedParam = "Alerted";

        [FoldoutGroup("Alerted"), SerializeField, LabelText("Param Type")]
        private ParamMode alertedMode = ParamMode.Bool;

        private int suspiciousHash;
        private int alertedHash;
    
        public void Initialize(Blackboard blackboard)
        {
            animator = blackboard.animator;
            animatorModule = blackboard.animatorModule;
        
            suspiciousHash = string.IsNullOrWhiteSpace(suspiciousParam) ? 0 : Animator.StringToHash(suspiciousParam);
            alertedHash = string.IsNullOrWhiteSpace(alertedParam) ? 0 : Animator.StringToHash(alertedParam);

        }

        public void OnAggroStateChanged(AggroStateChangeEventArgs args)
        {
            if (!animator) return;

            switch (args.NewState)
            {
                case AggroState.Passive:
                    ClearBool(suspiciousHash, suspiciousMode);
                    ClearBool(alertedHash, alertedMode);
                    break;

                case AggroState.Suspicious:
                    Apply(suspiciousHash, suspiciousMode);
                    ClearBool(alertedHash, alertedMode);
                    break;

                case AggroState.Alerted:
                    Apply(alertedHash, alertedMode);
                    ClearBool(suspiciousHash, suspiciousMode);
                    break;
            }
        }

        private void Apply(int hash, ParamMode mode)
        {
            if (hash == 0) return;

            if (mode == ParamMode.Trigger) animator.SetTrigger(hash);
            else animator.SetBool(hash, true);
        }

        private void ClearBool(int hash, ParamMode mode)
        {
            if (hash == 0) return;
            if (mode != ParamMode.Bool) return;

            animator.SetBool(hash, false);
        }
    }
}