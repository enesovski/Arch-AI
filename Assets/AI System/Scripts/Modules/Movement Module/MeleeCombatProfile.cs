using Sirenix.OdinInspector;
using UnityEngine;

namespace Archura
{
    public enum MeleePositioning { Front, Flank, Unrestricted }
    public enum MeleeRecoveryMode { MaintainDistance, HoldGround }

    [CreateAssetMenu(fileName = "Melee Combat Profile", menuName = "AI/Combat/Melee Combat Profile")]
    public sealed class MeleeCombatProfile : ScriptableObject
    {
        [Title("Positioning")]
        [Tooltip("Front: stays in the target's front sector. Flank: prefers the target's side/rear. Unrestricted: no angle limit. If the sector is unreachable, another angle may be used after Advanced > Blocked Sector Grace.")]
        public MeleePositioning positioning = MeleePositioning.Front;
        [ShowIf(nameof(IsFront)), Range(20f, 85f), Tooltip("Allowed angle on each side of the target's forward direction. 50 = a 100 degree front sector. Lower values keep the agent in a narrower front area; does not change wait times.")]
        public float frontHalfAngle = 70f;
        [ShowIf(nameof(IsFlank)), Range(90f, 180f), Tooltip("Preferred angle relative to the target's forward direction in Flank mode. 90 = side, 180 = rear. The agent moves within about 20 degrees of it.")]
        public float flankAngle = 120f;

        [Title("Spacing")]
        [HideIf(nameof(HoldsGround)), Min(0.1f), Tooltip("Maintain Distance mode: distance (m) between the target's horizontal body bounds and the agent's root while withdrawing or circling. Grows automatically if attack reach or agent radius requires it. Not used while holding ground.")]
        public float holdingDistance = 4.5f;

        [Title("Recovery")]
        [Tooltip("Maintain Distance: re-opens the gap as the target closes in. Hold Ground: takes one short step back after an attack and keeps its spot; does not retreat when the target approaches. Back step: Movement Profile > Withdrawal Step Distance; side steps: Circling Steps.")]
        public MeleeRecoveryMode recoveryMode;
        [Min(0f), Tooltip("Duration (s) of the circle/wait phase after withdrawing, including pauses between steps; cooldowns can extend it. In Hold Ground, if the target closes in by at least 0.5 m, a ready attack in range may trigger early.")]
        public float circleDuration = 1.5f;
        [Min(0f), Tooltip("Adds a random 0..value seconds to each circle phase, e.g. Duration 3 + Variance 0.5 = 3–3.5 s. Does not affect pauses between steps.")]
        public float circleDurationVariance = 0.5f;

        [FoldoutGroup("Advanced"), Min(0f), Tooltip("Minimum time (s) spent in the withdrawal phase even after reaching Holding Distance. The agent is not forced to keep moving during it.")]
        public float minimumWithdrawalTime = 0.6f;
        [FoldoutGroup("Advanced"), Min(0.1f), Tooltip("Maximum time (s) allowed for withdrawal. When it runs out the agent moves on to circling, even if the distance was not reached or the path is blocked.")]
        public float withdrawalTimeout = 2.5f;
        [FoldoutGroup("Advanced"), Min(0.1f), Tooltip("Time (s) to wait before allowing another angle when no reachable point exists in the preferred sector or angle correction makes no progress. Attack range and facing requirements still apply.")]
        public float blockedSectorGrace = 2f;
        [FoldoutGroup("Advanced"), Range(5f, 90f), Tooltip("Maximum angle between the agent's forward and the direction to the target for an attack to start. Smaller values require more precise facing. Unlike Front Half Angle, which limits position around the target.")]
        public float attackFacingHalfAngle = 45f;

        private bool IsFront => positioning == MeleePositioning.Front;
        private bool IsFlank => positioning == MeleePositioning.Flank;
        private bool HoldsGround => recoveryMode == MeleeRecoveryMode.HoldGround;
    }
}
