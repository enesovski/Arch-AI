using MoreMountains.Feedbacks;
using UnityEngine;

namespace Artifika.AI.Stun.Reactions
{
    [DisallowMultipleComponent]
    public sealed class StunFeedbackReaction : MonoBehaviour, IStunReaction
    {
        [SerializeField] private MMF_Player stunFeedback;
        [SerializeField] private bool stopFeedbackOnRecover = true;

        public void Initialize(Blackboard blackboard)
        {
        }

        public void OnStunnedChanged(bool isStunned)
        {
            if (stunFeedback == null) return;

            if (isStunned)
            {
                stunFeedback.PlayFeedbacks();
            }
            else if (stopFeedbackOnRecover)
            {
                stunFeedback.StopFeedbacks();
            }
        }
    }
}
