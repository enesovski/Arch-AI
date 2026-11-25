using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "Movement Profile", menuName = "AI/Movement Profile")]
    public class MovementProfile : ScriptableObject
    {
        public EntityType entityType = EntityType.Aggressive;

        private bool IsAggressive => entityType == EntityType.Aggressive || entityType == EntityType.Boss;
        private bool IsTamed => entityType == EntityType.Tamed;

        [Title("Wander")]
        [Tooltip("Normal walking speed (m/s). Also used for withdrawal and circling when their speeds are 0.")]
        public float walkMovementSpeed = 6f;

        [MinMaxSlider(0f, 50f, true)]
        [Tooltip("Min/max distance (m) for random Wander destinations. Does not affect combat holding distance or circle steps.")]
        public Vector2 wanderDistanceRange = new Vector2(12f, 30f);

        public float MinWanderDistance => wanderDistanceRange.x;
        public float MaxWanderDistance => wanderDistanceRange.y;

        [Title("Run")]
        [Tooltip("Speed (m/s) while approaching and rushing. Does not affect circle or withdrawal speeds.")]
        public float runMovementSpeed = 8f;
        [Tooltip("Distance range (m) for FleeFromTarget destinations. Unrelated to melee rush speed or step pauses.")]
        public float runDistanceInterval = 2f;

        [TitleGroup("Combat Movement")]
        [LabelText("Withdrawal Speed (m/s)"), Min(0f), Tooltip("Withdrawal speed after an attack (m/s); 0 uses Walk Movement Speed. Distance comes from Holding Distance (Maintain Distance) or Withdrawal Step Distance (Hold Ground).")]
        public float combatWithdrawalSpeed;

        [TitleGroup("Combat Movement")]
        [LabelText("Withdrawal Step Distance (m)"), Min(0f)]
        [Tooltip("Hold Ground mode: maximum single step back (m) after an attack. Not extended when the target approaches; 0 disables it. Shorter or no step when blocked behind. Not used in Maintain Distance mode.")]
        public float combatWithdrawalStepDistance = 2f;

        [TitleGroup("Combat Movement")]
        [LabelText("Circling Speed (m/s)"), Min(0f), Tooltip("Sideways speed while circling (m/s); 0 uses Walk Movement Speed. Does not change pauses or the total circle duration.")]
        public float combatCirclingSpeed;

        [FoldoutGroup("Combat Movement/Circling Steps")]
        [LabelText("Step And Pause"), Tooltip("While circling, walks to a short goal and pauses on arrival instead of moving continuously. Face Target applies while paused. A ready attack or distance/angle correction can cut the pause short.")]
        public bool combatStepCircling;

        [FoldoutGroup("Combat Movement/Circling Steps"), ShowIf(nameof(combatStepCircling))]
        [LabelText("Step Distance (m)"), Min(0.1f), Tooltip("Maintain Distance: approximate upper bound of a side step around the target; distance/angle correction may exceed it. Hold Ground: moves within a strip of this length on both sides of the anchored spot; the strip does not shift as the target approaches. Very small values may produce no movement because of Waypoint Tolerance.")]
        public float combatCircleStepDistance = 1.5f;

        [FoldoutGroup("Combat Movement/Circling Steps"), ShowIf(nameof(combatStepCircling))]
        [LabelText("Pause Between Steps (s)"), MinMaxSlider(0f, 5f, true)]
        [Tooltip("Random pause after each short step, e.g. 0.8–1.5 s; no pause when both are 0. Does not change attack cooldowns; pauses count toward Circle Duration.")]
        public Vector2 combatCirclePauseRange = new Vector2(0.8f, 1.5f);

        [TitleGroup("Combat Movement")]
        [LabelText("Attack Approach Spacing"), Range(0.5f, 0.95f)]
        [Tooltip("Fraction of the usable melee reach at which a rush stops. 0.9 = 90%; higher values stop farther away. Does not change damage range or withdrawal distance.")]
        public float combatApproachRangeFraction = 0.75f;

        [TitleGroup("Combat Movement")]
        [LabelText("Face Target While Repositioning"), Tooltip("Faces the target while withdrawing, circling and pausing between steps; otherwise faces the movement direction. Requires strafe/backward walk animations.")]
        public bool combatFaceTargetWhileRepositioning = true;

        [FoldoutGroup("Combat Movement/Advanced")]
        [LabelText("Orbit Goal Speed (degrees/s)"), Range(5f, 60f), Tooltip("Maintain Distance: angular speed of the circle goal around the target. Actual movement is limited by Circling Speed and Step Distance. Not used for Hold Ground side steps.")]
        public float combatCircleDegreesPerSecond = 25f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Target Facing Follow Speed (degrees/s)"), Min(1f)]
        [Tooltip("How fast the preferred front/side sector follows the target's rotation. Not the agent's turn speed, which the Rotation Module controls.")]
        public float combatFacingFollowSpeed = 120f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Replan Interval (s)"), Min(0.1f)]
        [Tooltip("Seconds between positioning decisions. Lower values react faster but compute paths more often. Stepped circling keeps its current step goal and does not path while paused.")]
        public float combatReplanInterval = 0.35f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Destination Change Threshold (m)"), Min(0.1f)]
        [Tooltip("Keeps the current path when the new goal moved less than this. Higher values repath less for small changes. Completed circle steps are not held back by it.")]
        public float combatDestinationChangeThreshold = 0.5f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Waypoint Tolerance (m)"), Min(0.1f)]
        [Tooltip("Arrival radius (m) for circle/withdrawal goals; higher values end steps earlier. Rush uses at most 0.05 m so it never stops outside attack range.")]
        public float combatWaypointTolerance = 0.3f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("NavMesh Sample Radius (m)"), Min(0.1f)]
        [Tooltip("Search radius for a walkable NavMesh point around a planned position. Too small fails on narrow or sloped ground; too large can drift from the intended spot. Only fully reachable paths are accepted.")]
        public float combatNavMeshSampleRadius = 1f;

        public float GetCombatWithdrawalSpeed() => Mathf.Max(0.1f, combatWithdrawalSpeed > 0f ? combatWithdrawalSpeed : walkMovementSpeed);
        public float GetCombatCirclingSpeed() => Mathf.Max(0.1f, combatCirclingSpeed > 0f ? combatCirclingSpeed : walkMovementSpeed);

        [TitleGroup("Ranged Movement"), Min(0f)]
        [Tooltip("Sideways repositioning speed between ranged attacks (m/s); 0 uses Walk Movement Speed. Approaching a distant target uses Run Movement Speed.")]
        public float rangedRepositionSpeed = 6f;

        [TitleGroup("Ranged Movement"), Min(0f)]
        [Tooltip("Speed of the short evasion step when the target gets too close (m/s); 0 uses Run Movement Speed. The trigger distance lives in the Ranged Combat Profile.")]
        public float rangedRetreatSpeed = 9f;

        [TitleGroup("Ranged Movement"), Min(0.1f)]
        [Tooltip("Approximate side step length between shots (m). Range correction or finding a clear shot may pick a different spot. The goal is not shifted every frame once chosen.")]
        public float rangedRepositionStepDistance = 3f;

        [TitleGroup("Ranged Movement"), Min(0.1f)]
        [Tooltip("Maximum single back/side evasion step when the target closes in (m). Not extended while being chased; a shorter or sideways step is tried when blocked.")]
        public float rangedRetreatStepDistance = 4f;

        [TitleGroup("Combat Tactics", VisibleIf = nameof(IsAggressive))]
        [Tooltip("Side offset (m) for the StrafeTarget graph action. Melee circle steps use Circling Steps > Step Distance instead.")]
        public float strafeDistance = 4f;

        [TitleGroup("Combat Tactics", VisibleIf = nameof(IsAggressive))]
        [Tooltip("Distance (m) around the target for the FlankTarget graph action. Unrelated to the Melee Combat Profile's flank angle or Holding Distance.")]
        public float flankDistance = 6f;

        [TitleGroup("Search", VisibleIf = nameof(IsAggressive))]
        public float searchMovementSpeed = 6f;

        [TitleGroup("Search", VisibleIf = nameof(IsAggressive))]
        public float lookAroundDuration = 2f;

        [TitleGroup("Search", VisibleIf = nameof(IsAggressive))]
        [MinMaxSlider(0f, 20f, true)]
        public Vector2 searchDistanceRange = new Vector2(4f, 7f);

        [TitleGroup("Follow", VisibleIf = nameof(IsTamed))]
        public float teleportRadius = 70f;

        [TitleGroup("Follow", VisibleIf = nameof(IsTamed))]
        public float stopRadius = 8f;

        [TitleGroup("Follow", VisibleIf = nameof(IsTamed))]
        public float followRadius = 18f;

        public float MinSearchDistance => searchDistanceRange.x;
        public float MaxSearchDistance => searchDistanceRange.y;
    }
}
