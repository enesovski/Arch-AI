using System;
using MoreMountains.Feedbacks; 
using UnityEngine;

namespace Artifika.AI.Death.Reactions
{
    [Serializable]
    public sealed class PlayFeedbackReaction : IDeathReaction
    {
        [SerializeField] private MMF_Player feedback;
        
        public void Initialize(Blackboard blackboard)
        {
        }

        public void Execute()
        {
            feedback?.PlayFeedbacks();
        }
    }
}