using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Attack
{
    public enum AttackType { Melee, Ranged, Dash}

    public abstract class BaseAttackDefinition : ScriptableObject
    {
        [TabGroup("Attack", "General")]
        [Title("General", TitleAlignment = TitleAlignments.Centered)]

        [PropertyOrder(0)]
        [LabelText("Animation Index")]
        [MinValue(0)]
        [SerializeField] private int animationIndex;

        [PropertyOrder(1)]
        [LabelText("Type")]
        [EnumToggleButtons]
        [SerializeField] private AttackType attackType;

        [PropertyOrder(2)]
        [BoxGroup("Attack/General/Range", ShowLabel = true)]
        [HorizontalGroup("Attack/General/Range/H", Width = 0.5f)]
        [LabelText("Min")]
        [MinValue(0)]
        [SerializeField] private float minRange;

        [HorizontalGroup("Attack/General/Range/H", Width = 0.5f)]
        [LabelText("Max")]
        [MinValue(0)]
        [SerializeField] private float maxRange;

        [PropertyOrder(3)]
        [BoxGroup("Attack/General/Timing", ShowLabel = true)]
        [MinValue(0)]
        [LabelText("Cooldown (s)")]
        [SuffixLabel("sec", true)]
        [SerializeField] private float cooldown;

        [BoxGroup("Attack/General/Timing")]
        [MinValue(0)]
        [LabelText("Telegraph (s)")]
        [Tooltip("Wind-up duration for telegraphing (seconds).")]
        [SuffixLabel("sec", true)]
        [SerializeField] private float telegraphTime;

        [BoxGroup("Attack/General/Timing")]
        [MinValue(0)]
        [LabelText("Execute (s)")]
        [Tooltip("Active window duration (seconds).")]
        [SuffixLabel("sec", true)]
        [SerializeField] private float executeTime;

        [BoxGroup("Attack/General/Timing")]
        [MinValue(0)]
        [LabelText("Recovery (s)")]
        [Tooltip("Recovery delay after execution (seconds).")]
        [SuffixLabel("sec", true)]
        [SerializeField] private float recoveryTime;

        [PropertyOrder(4)]
        [BoxGroup("Attack/General/Movement", ShowLabel = true)]
        [LabelText("Stop During Attack")]
        [Tooltip("Movement behavior during the attack.")]
        [SerializeField] private bool stopDuringAttack = true;
        
        [BoxGroup("Attack/General/Movement", ShowLabel = true)]
        [LabelText("Look At Target During Attack")]
        public bool lookAtTargetDuringAttack;

        [BoxGroup("Attack/General/Timing")]
        [ShowInInspector, ReadOnly]
        [LabelText("Total Duration")]
        [SuffixLabel("sec", true)]
        private float TotalDuration => telegraphTime + executeTime + recoveryTime;


        [TabGroup("Attack", "Scoring")]
        [Title("Utility Scoring", TitleAlignment = TitleAlignments.Centered)]

        [PropertyOrder(0)]
        [BoxGroup("Attack/Scoring/Curves", ShowLabel = true)]
        [LabelText("Distance Utility")]
        [Tooltip("Input: normalized distance (0..1). Output: utility score.")]
        [SerializeField] private AnimationCurve distanceUtilityCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [BoxGroup("Attack/Scoring/Curves")]
        [LabelText("Health Utility")]
        [Tooltip("Input: normalized health (0..1). Output: utility score.")]
        [SerializeField] private AnimationCurve healthUtilityCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [PropertyOrder(1)]
        [BoxGroup("Attack/Scoring/Weights", ShowLabel = true)]
        [LabelText("Cooldown Penalty")]
        [MinValue(0)]
        [SerializeField] private float cooldownPenalty = 1f;

        [PropertyOrder(2)]
        [BoxGroup("Attack/Scoring/Variance", ShowLabel = true)]
        [LabelText("Random Variance")]
        [MinValue(0)]
        [SerializeField] private float randomUtilityVariance = 0.1f;

        [BoxGroup("Attack/Scoring/Variance")]
        [LabelText("Additional Variance")]
        [MinValue(0)]
        [SerializeField] private float additionalUtilityVariance = 0f;


#if UNITY_EDITOR
        [TabGroup("Attack", "General")]
        [InfoBox("Min Range should be <= Max Range.", InfoMessageType.Warning, nameof(IsRangeInvalid))]
        [InfoBox("Cooldown / Telegraph / Execute / Recovery should be >= 0.", InfoMessageType.Error, nameof(IsTimingInvalid))]
        private bool IsRangeInvalid => minRange > maxRange;
        private bool IsTimingInvalid => cooldown < 0 || telegraphTime < 0 || executeTime < 0 || recoveryTime < 0;
#endif

        public int AnimationIndex => animationIndex;
        public AttackType AttackType => attackType;
        public float MinRange => minRange;
        public float MaxRange => maxRange;
        public float Cooldown => cooldown;
        public float TelegraphTime => telegraphTime;
        public float ExecuteTime => executeTime;
        public float RecoveryTime => recoveryTime;
        public bool StopDuringAttack => stopDuringAttack;

        public AnimationCurve DistanceUtilityCurve => distanceUtilityCurve;
        public AnimationCurve HealthUtilityCurve => healthUtilityCurve;
        public float CooldownPenalty => cooldownPenalty;
        public float RandomUtilityVariance => randomUtilityVariance;

        public float AdditionalUtilityScore => additionalUtilityVariance;
    }
}
