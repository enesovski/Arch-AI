using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Attack
{
    public enum AttackType { Melee, Ranged, Dash }

    public abstract class BaseAttackDefinition : ScriptableObject
    {
        [TabGroup("Attack", "General")]
        [Title("General", TitleAlignment = TitleAlignments.Centered)]

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

        [PropertyOrder(4)]
        [BoxGroup("Attack/General/Cooldown", ShowLabel = true)]
        [MinValue(0)]
        [LabelText("Cooldown (s)")]
        [SuffixLabel("sec", true)]
        [SerializeField] private float cooldown;

        [PropertyOrder(5)]
        [BoxGroup("Attack/General/Movement", ShowLabel = true)]
        [LabelText("Stop During Attack")]
        [Tooltip("Movement behavior during the attack.")]
        [SerializeField] private bool stopDuringAttack = true;

        [BoxGroup("Attack/General/Movement")]
        [LabelText("Look At Target Mode")]
        [SerializeField] private AttackLookAtTargetMode lookAtTargetMode = AttackLookAtTargetMode.TrackTarget;

        [TabGroup("Attack", "Animation")]
        [Title("Animation", TitleAlignment = TitleAlignments.Centered)]
        [PropertyOrder(0)]
        [LabelText("Animation Clip")]
        [SerializeField] private AnimationClip animationClip;

        [PropertyOrder(0)]
        [LabelText("Animation Index")]
        [MinValue(0)]
        [SerializeField] private int animationIndex;

        [SerializeField, HideInInspector] private float telegraphTime;
        [SerializeField, HideInInspector] private float executeTime;
        [SerializeField, HideInInspector] private float recoveryTime;

        [TabGroup("Attack", "Animation")]
        [PropertyOrder(1)]
        [BoxGroup("Attack/Animation/Duration", ShowLabel = true)]
        [InfoBox("AI attack readiness uses the animation clip duration. Each animation event can execute one hit/projectile.")]
        [ShowInInspector, ReadOnly]
        [LabelText("Attack Duration")]
        [SuffixLabel("sec", true)]
        private float InspectorAttackDuration => AttackDuration;

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
        [BoxGroup("Attack/Scoring/Variance", ShowLabel = true)]
        [LabelText("Additional Utility")]
        [MinValue(0)]
        [SerializeField] private float additionalUtilityVariance = 0f;

#if UNITY_EDITOR
        [TabGroup("Attack", "General")]
        [InfoBox("Min Range should be <= Max Range.", InfoMessageType.Warning, nameof(IsRangeInvalid))]
        private bool IsRangeInvalid => minRange > maxRange;

#endif

        public int AnimationIndex => animationIndex;

        public AttackType AttackType => attackType;

        public float MinRange => minRange;
        public float MaxRange => maxRange;

        public float Cooldown => cooldown;
        public AttackLookAtTargetMode LookAtTargetMode => lookAtTargetMode;
        public float TelegraphTime => telegraphTime;
        public float ExecuteTime => executeTime;
        public float RecoveryTime => recoveryTime;
        public float ConfiguredDuration => telegraphTime + executeTime + recoveryTime;
        public float AttackDuration => animationClip != null
            ? animationClip.length
            : Mathf.Max(0.1f, ConfiguredDuration);

        public bool StopDuringAttack => stopDuringAttack;

        public AnimationCurve DistanceUtilityCurve => distanceUtilityCurve;
        public AnimationCurve HealthUtilityCurve => healthUtilityCurve;

        public float AdditionalUtilityScore => additionalUtilityVariance;
    }
}
