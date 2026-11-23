using Archura.Attack;
using Sirenix.OdinInspector;
using Unity.Behavior;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [DisallowMultipleComponent]
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public class Blackboard : MonoBehaviour
    {
        #region Inspector Properties

        private bool IsTamed => gameEntity != null && gameEntity.EntityType == EntityType.Tamed;
        private bool IsTameable => gameEntity != null && gameEntity.entityConfiguration != null && gameEntity.entityConfiguration.isTameable;

        [TitleGroup("Modules")]
        public AttackModule attackModule;
        public DetectionModule detectionModule;
        public MovementModule movementModule;
        public AggroModule aggroModule;
        public AnimatorModule animatorModule;
        public RotationModule rotationModule;

        [TitleGroup("Components")] public GameEntity gameEntity;
        public BehaviorGraphAgent behaviorGraphAgent;

        [Tooltip("Component implementing " + nameof(IEntityHealth) + ". Resolved from the hierarchy when empty.")]
        [FormerlySerializedAs("healthComponent")]
        [SerializeField] private MonoBehaviour healthSource;

        [Tooltip("Optional component implementing " + nameof(IGroundFitter) + ".")]
        [FormerlySerializedAs("fitter")]
        [SerializeField] private MonoBehaviour groundFitterSource;

        public NavMeshAgent agent;
        public Animator animator;

        [Tooltip("Optional component implementing " + nameof(IStunState) + ". Resolved from the hierarchy when empty.")]
        [FormerlySerializedAs("entityStatsController")]
        [SerializeField] private MonoBehaviour stunSource;

        [Title("Busses")] public ReactionBusBase[] reactions;

        [Title("Configuration")] public float nestRadius = 40f;

        [Tooltip("The number of attackers at which this AI decides the target is too crowded and prefers to strafe/reposition.")]
        [Range(1, 10)] public int maxExpectedAggro = 3;

        [TitleGroup("Tameable", VisibleIf = nameof(IsTameable))]

        [TitleGroup("Tamed", VisibleIf = nameof(IsTamed))]
        public CompanionStateModule companionStateModule;

        [FormerlySerializedAs("companionCommandModule")] [TitleGroup("Tamed")]
        public CommandModule commandModule;

        [Title("Tactical Context")] [Tooltip("Distance at which NormalizedDistance = 1.")] [SerializeField]
        private float maxDetectionRange = 20f;

        [Tooltip("Raw threat score from DetectionModule that maps to ThreatScore = 1.")] [SerializeField]
        private float maxThreatScore = 10f;

        [Tooltip("TimeSinceLastAttack that maps to a normalized value of 1.")] [SerializeField]
        private float maxTimeSinceAttack = 10f;

        #endregion

        #region Host Integration

        public IEntityHealth Health
        {
            get
            {
                if (UnityNull.IsNull(healthSource as IEntityHealth))
                    healthSource = GetReferenceInParentOrChildren<IEntityHealth>() as MonoBehaviour;
                return healthSource as IEntityHealth;
            }
        }

        public IStunState Stun
        {
            get
            {
                if (UnityNull.IsNull(stunSource as IStunState))
                    stunSource = GetReferenceInParentOrChildren<IStunState>() as MonoBehaviour;
                return stunSource as IStunState;
            }
        }

        public IGroundFitter Fitter => groundFitterSource as IGroundFitter;
        public Behaviour FitterBehaviour => groundFitterSource;

        #endregion

        public void Initialize()
        {
            attackModule?.SetBlackboard(this);
            detectionModule?.SetBlackboard(this);
            movementModule?.SetBlackboard(this);
            aggroModule?.SetBlackboard(this);
            rotationModule?.Initialize(agent, Fitter);
            Fitter?.Initialize(this);

            foreach (var reactionBus in reactions)
                reactionBus.Initialize(this);
        }

        #region Public Properties

        public bool HasDetected(GameEntity sourceEntity)
        {
            return detectionModule && detectionModule.HasDetected(sourceEntity);
        }

        public bool IsAlive()
        {
            IEntityHealth health = Health;
            if (UnityNull.IsNull(health)) return true;
            return health.IsAlive;
        }

        public bool IsStunned()
        {
            IStunState stun = Stun;
            return !UnityNull.IsNull(stun) && stun.IsStunned;
        }

        public bool CanBeTamed()
        {
            return IsAlive()
                && IsStunned()
                && gameEntity.entityConfiguration != null
                && gameEntity.entityConfiguration.isTameable
                && gameEntity.entityConfiguration.tamedPrefab != null;
        }

        public IEntityStateModule GetStateModule()
        {
            return aggroModule;
        }

        #endregion

        #region Tactical Context

        public TacticalContext GetTacticalContext()
        {
            GameEntity target = aggroModule?.CurrentTarget;
            bool hasTarget = target != null;

            int targetAggroCount = 0;
            if (hasTarget && target.AggroTracker != null)
            {
                targetAggroCount = target.AggroTracker.AggroEntities.Count;
            }

            Vector3 selfPos = transform.position;
            Vector3 targetPos = hasTarget ? target.transform.position : selfPos;
            float distance = hasTarget ? Vector3.Distance(selfPos, targetPos) : 0f;

            IEntityHealth ownHp = Health;
            float ownHealth = !UnityNull.IsNull(ownHp)
                ? Mathf.Clamp01(ownHp.Health / ownHp.MaxHealth)
                : 1f;

            float targetHealth = hasTarget && target.TryGetComponent<IEntityHealth>(out var targetHp)
                ? Mathf.Clamp01(targetHp.Health / targetHp.MaxHealth)
                : 1f;

            float rawThreat = hasTarget && detectionModule != null
                ? detectionModule.GetThreatScore(target)
                : 0f;

            float timeSinceAttack = attackModule != null
                ? attackModule.TimeSinceLastAttack
                : 0f;

            bool hasLoS = hasTarget && detectionModule != null
                && detectionModule.HasLineOfSight(selfPos, targetPos);

            return new TacticalContext
            {
                SelfPosition = selfPos,
                TargetPosition = targetPos,
                HasTarget = hasTarget,
                Distance = distance,
                NormalizedDistance = Mathf.Clamp01(distance / maxDetectionRange),
                OwnHealthNormalized = ownHealth,
                TargetHealthNormalized = targetHealth,
                ThreatScore = maxThreatScore > 0f ? Mathf.Clamp01(rawThreat / maxThreatScore) : 0f,
                CanAttack = attackModule != null && attackModule.CanStartAttack(),
                CanStartAttack = attackModule != null && attackModule.CanStartAttack(),
                HasLineOfSight = hasLoS,
                TimeSinceLastAttack = timeSinceAttack,
                TargetAggroCount = targetAggroCount,
                MaxExpectedAggro = maxExpectedAggro,
                Aggressiveness = aggroModule != null && aggroModule.configuration != null ? aggroModule.configuration.aggressiveness : 0.5f
            };
        }

        #endregion

        private T GetReferenceInParentOrChildren<T>() where T : class
        {
            T component = GetComponent<T>();

            if (component != null)
                return component;

            component = GetComponentInParent<T>(true);

            if (component != null)
                return component;

            return transform.root.GetComponentInChildren<T>(true);
        }

#if UNITY_EDITOR
        [TitleGroup("Editor Tools")]
        [Button(ButtonSizes.Large)]
        [GUIColor(0.35f, 0.8f, 1f)]
        private void GatherReferences()
        {
            Undo.RecordObject(this, "Gather Blackboard References");

            attackModule = GetReferenceInChildren<AttackModule>();
            detectionModule = GetReferenceInChildren<DetectionModule>();
            movementModule = GetReferenceInChildren<MovementModule>();
            aggroModule = GetReferenceInChildren<AggroModule>();
            animatorModule = GetReferenceInChildren<AnimatorModule>();
            rotationModule = GetReferenceInChildren<RotationModule>();

            gameEntity = GetReferenceInParentOrChildren<GameEntity>();
            behaviorGraphAgent = GetReferenceInChildren<BehaviorGraphAgent>();
            healthSource = GetReferenceInParentOrChildren<IEntityHealth>() as MonoBehaviour;
            groundFitterSource = GetReferenceInChildren<IGroundFitter>() as MonoBehaviour;
            agent = GetReferenceInChildren<NavMeshAgent>();
            animator = GetReferenceInChildren<Animator>();
            stunSource = GetReferenceInParentOrChildren<IStunState>() as MonoBehaviour;

            companionStateModule = GetReferenceInChildren<CompanionStateModule>();
            commandModule = GetReferenceInChildren<CommandModule>();

            reactions = GetComponentsInChildren<ReactionBusBase>(true);

            EditorUtility.SetDirty(this);
        }

        private T GetReferenceInChildren<T>() where T : class
        {
            return GetComponentInChildren<T>(true);
        }

        private void OnValidate()
        {
            WarnIfMissingContract<IEntityHealth>(healthSource, nameof(healthSource));
            WarnIfMissingContract<IGroundFitter>(groundFitterSource, nameof(groundFitterSource));
            WarnIfMissingContract<IStunState>(stunSource, nameof(stunSource));
        }

        private void WarnIfMissingContract<T>(MonoBehaviour source, string field) where T : class
        {
            if (source != null && source is not T)
                Debug.LogWarning($"[{nameof(Blackboard)}] '{field}' on {name} must implement {typeof(T).Name}; " +
                                 $"{source.GetType().Name} does not.", this);
        }

#endif
    }
}
