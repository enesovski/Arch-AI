using UnityEngine;
using Sirenix.OdinInspector;

namespace Artifika.AI.Aggro
{
    [CreateAssetMenu(fileName = "AggroPolicy", menuName = "AI/Aggro/Aggro Policy")]
    public sealed class AggroPolicy : ScriptableObject
    {
        [Tooltip("Aggressive creatures register aggro on their target. Passive creatures do not.")]
        public EngagementMode mode = EngagementMode.Aggressive;

        [FoldoutGroup("Thresholds"), MinValue(0f)]
        [Tooltip("Threat score required to ENTER Alert from Idle.")]
        public float alertEnterThreshold = 0.25f;

        [FoldoutGroup("Thresholds"), MinValue(0f)]
        [Tooltip("Threat score below which Alert drops back to Idle. Must be < alertEnterThreshold.")]
        public float alertExitThreshold = 0.15f;

        [FoldoutGroup("Thresholds"), MinValue(0f)]
        [Tooltip("Threat score required to enter Combat. Must be > alertEnterThreshold.")]
        public float engageThreshold = 0.6f;

        [FoldoutGroup("Search"), MinValue(0f)]
        [Tooltip("Seconds the target can be unseen before considered 'hard lost' (triggers Search).")]
        public float lostGraceSeconds = 1.25f;

        [FoldoutGroup("Search"), MinValue(0f)]
        [Tooltip("How long the AI searches before giving up and returning to Idle.")]
        public float searchDurationSeconds = 8f;

        [FoldoutGroup("Engaging"), MinValue(0.1f)]
        [Tooltip("Max distance at which the AI can enter Combat.")]
        public float engageDistance = 25f;

        [FoldoutGroup("Engaging")]
        [Tooltip("If true, target must be visible (not just detected via hit) to enter Combat.")]
        public bool requireVisibilityToEngage = true;

        [FoldoutGroup("Stability"), MinValue(0f)]
        [Tooltip("Minimum seconds to hold Combat state before evaluating transitions.")]
        public float minEngageHoldSeconds = 0.75f;

        [FoldoutGroup("Stability")]
        [Tooltip("If true, Combat state is held while an attack animation is committed.")]
        public bool keepEngageWhileAttackCommitted = true;

        [FoldoutGroup("Stability"), MinValue(0f)]
        [Tooltip("Targets within this range are always considered visible.")]
        public float closeRangeAssumeVisibleDistance = 2.5f;

        private void OnValidate()
        {
            alertEnterThreshold = Mathf.Max(0f, alertEnterThreshold);
            alertExitThreshold = Mathf.Clamp(alertExitThreshold, 0f, alertEnterThreshold);
            engageThreshold = Mathf.Max(alertEnterThreshold, engageThreshold);

            lostGraceSeconds = Mathf.Max(0f, lostGraceSeconds);
            searchDurationSeconds = Mathf.Max(0f, searchDurationSeconds);

            engageDistance = Mathf.Max(0.1f, engageDistance);
            minEngageHoldSeconds = Mathf.Max(0f, minEngageHoldSeconds);
            closeRangeAssumeVisibleDistance = Mathf.Max(0f, closeRangeAssumeVisibleDistance);
        }
    }
}