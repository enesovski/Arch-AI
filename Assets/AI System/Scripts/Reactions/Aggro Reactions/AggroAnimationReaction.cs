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

        [FoldoutGroup("Suspicious"), SerializeField]
        private string suspiciousParam = "Suspicious";

        [FoldoutGroup("Suspicious"), SerializeField, LabelText("Param Type")]
        private ParamMode suspiciousMode = ParamMode.Bool;

        [FoldoutGroup("Alerted"), SerializeField]
        private string alertedParam = "Alerted";

        [FoldoutGroup("Alerted"), SerializeField, LabelText("Param Type")]
        private ParamMode alertedMode = ParamMode.Bool;

        [FoldoutGroup("Suspicious Sub"), SerializeField]
        [Tooltip("Set true only while SuspiciousState.Alerted — agent stops and observes.")]
        private string suspiciousLookParam = "SuspiciousLook";

        private int suspiciousHash;
        private int alertedHash;
        private int suspiciousLookHash;

        public void Initialize(Blackboard blackboard)
        {
            animator = blackboard.animator;

            if (!animator) return;

            suspiciousHash     = ValidatedHash(suspiciousParam);
            alertedHash        = ValidatedHash(alertedParam);
            suspiciousLookHash = ValidatedHash(suspiciousLookParam);
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
                    ClearBool(suspiciousLookHash, ParamMode.Bool);
                    break;
            }
        }

        public void OnSuspiciousStateChanged(SuspiciousStateChangeEventArgs args)
        {
            if (!animator || suspiciousLookHash == 0) return;
            animator.SetBool(suspiciousLookHash, args.NewState == SuspiciousState.Alerted);
        }

        private void Apply(int hash, ParamMode mode)
        {
            if (hash == 0) return;
            if (mode == ParamMode.Trigger) animator.SetTrigger(hash);
            else animator.SetBool(hash, true);
        }

        private void ClearBool(int hash, ParamMode mode)
        {
            if (hash == 0 || mode != ParamMode.Bool) return;
            animator.SetBool(hash, false);
        }

        private int ValidatedHash(string param)
        {
            if (string.IsNullOrWhiteSpace(param)) return 0;

            int hash = Animator.StringToHash(param);
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.nameHash == hash) return hash;

            return 0;
        }
    }
}