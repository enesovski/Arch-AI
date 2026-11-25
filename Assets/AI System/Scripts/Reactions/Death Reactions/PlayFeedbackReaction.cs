using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Archura.Death.Reactions
{
    [Serializable]
    public sealed class PlayFeedbackReaction : IDeathReaction
    {
        [SerializeField] private MMF_Player feedback;

        public void Execute()
        {
            if (feedback)
                feedback.PlayFeedbacks();
        }
    }
}
