using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Aggro
{
    [CreateAssetMenu(fileName = "Aggro Configuration", menuName = "AI/Aggro/Aggro Config")]
    public sealed class AggroConfiguration : ScriptableObject
    {
        [Title("Engagement Mode")]
        [Tooltip("Aggressive creatures register aggro on their target. Passive creatures do not.")]
        public AggressivenessMode mode = AggressivenessMode.Aggressive;

        [Title("Thresholds")]
        [MinValue(0f)]
        [Tooltip("Threat score required to enter Alerted (bottom of Suspicious) from Passive.")]
        public float alertEnterThreshold = 0.25f;

        [MinValue(0f)]
        [Tooltip("Threat score below which Suspicious drops back to Passive. Must be <= alertEnterThreshold.")]
        public float alertExitThreshold = 0.15f;

        [MinValue(0f)] [Tooltip("Threat score required to enter Engaged.")]
        public float engageThreshold = 0.6f;

        [Title("Threat Scoring")] [MinValue(0f)] [Tooltip("Multiplier applied to hit-based threat contribution.")]
        public float hitMultiplier = 2f;

        [MinValue(0f)] [Tooltip("Rate at which hit-based threat decays per second.")]
        public float hitDecayRate = 0.2f;

        [Title("Memory")] [MinValue(0f)] [Tooltip("Seconds after last sight before memory score decay begins.")]
        public float memoryDecayStartTime = 3f;

        [MinValue(0.01f)] [Tooltip("Duration in seconds for memory to decay from full score to zero.")]
        public float memoryDecayDuration = 5f;

        [Title("Search")]
        [MinValue(0f)]
        [Tooltip("Seconds the target can be unseen before considered hard-lost and Searching begins.")]
        public float lostGraceSeconds = 1.25f;

        [MinValue(0f)]
        [Tooltip("How long aggressive AI cautiously approaches a suspicious source before searching around it.")]
        public float suspiciousApproachSeconds = 4f;

        [MinValue(0.1f)]
        [Tooltip("Distance from the suspicious source where cautious approach stops and search begins.")]
        public float suspiciousApproachStopDistance = 7f;

        [MinValue(0.1f)]
        [Tooltip("Maximum distance the AI may travel during the cautious suspicious approach.")]
        public float suspiciousApproachMaxDistance = 18f;

        [MinValue(0f)] [Tooltip("How long the AI searches the last known position before returning to Passive.")]
        public float searchDurationSeconds = 8f;

        [Title("Engaging")] [MinValue(0.1f)] [Tooltip("Max distance at which the AI can enter Engaged.")]
        public float engageDistance = 25f;

        [Tooltip("If true, the target must be visible (not just hit-detected) to enter Engaged.")]
        public bool requireVisibilityToEngage = true;

        [Title("Stability")]
        [MinValue(0f)]
        [Tooltip("Minimum seconds to hold Engaged state before re-evaluating transitions.")]
        public float minEngageHoldSeconds = 0.75f;

        [Tooltip("If true, Engaged state is held for as long as an attack animation is committed.")]
        public bool keepEngageWhileAttackCommitted = true;

        [MinValue(0f)]
        [Tooltip("How much stronger another target's threat score must be before an engaged AI switches targets.")]
        public float targetSwitchScoreMargin = 0.25f;

        [MinValue(0f)]
        [Tooltip("Minimum seconds between target switches while already engaged.")]
        public float targetSwitchCooldownSeconds = 1f;

        [MinValue(0f)] [Tooltip("Targets within this range are always treated as visible regardless of LoS.")]
        public float closeRangeAssumeVisibleDistance = 2.5f;

        [Title("Tactics")]
        [Range(0f, 1f)]
        [Tooltip(
            "Base aggressiveness. Influences utility action scoring (higher = attack/chase, lower = kite/strafe).")]
        public float aggressiveness = 0.5f;

        private void OnValidate()
        {
            alertEnterThreshold = Mathf.Max(0f, alertEnterThreshold);
            alertExitThreshold = Mathf.Clamp(alertExitThreshold, 0f, alertEnterThreshold);

            engageThreshold = Mathf.Max(alertEnterThreshold, engageThreshold);

            lostGraceSeconds = Mathf.Max(0f, lostGraceSeconds);
            suspiciousApproachSeconds = Mathf.Max(0f, suspiciousApproachSeconds);
            suspiciousApproachStopDistance = Mathf.Max(0.1f, suspiciousApproachStopDistance);
            suspiciousApproachMaxDistance = Mathf.Max(0.1f, suspiciousApproachMaxDistance);
            searchDurationSeconds = Mathf.Max(0f, searchDurationSeconds);
            engageDistance = Mathf.Max(0.1f, engageDistance);
            minEngageHoldSeconds = Mathf.Max(0f, minEngageHoldSeconds);
            targetSwitchScoreMargin = Mathf.Max(0f, targetSwitchScoreMargin);
            targetSwitchCooldownSeconds = Mathf.Max(0f, targetSwitchCooldownSeconds);
            closeRangeAssumeVisibleDistance = Mathf.Max(0f, closeRangeAssumeVisibleDistance);
            memoryDecayStartTime = Mathf.Max(0f, memoryDecayStartTime);
            memoryDecayDuration = Mathf.Max(0.01f, memoryDecayDuration);
            hitMultiplier = Mathf.Max(0f, hitMultiplier);
            hitDecayRate = Mathf.Max(0f, hitDecayRate);
        }
    }
}
