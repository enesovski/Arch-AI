using Archura.Attack;
using Archura.Utility;
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

        [TitleGroup("Tamed", VisibleIf = nameof(IsTamed))]
        public CompanionStateModule companionStateModule;

        [FormerlySerializedAs("companionCommandModule")] [TitleGroup("Tamed")]
        public CommandModule commandModule;

        #endregion

        #region Host Integration

        private bool hasResolvedHealth;
        private bool hasResolvedStun;

        public IEntityHealth Health
        {
            get
            {
                if (!hasResolvedHealth)
                {
                    if (UnityNull.IsNull(healthSource as IEntityHealth))
                        healthSource = GetReferenceInParentOrChildren<IEntityHealth>() as MonoBehaviour;
                    hasResolvedHealth = true;
                }
                return healthSource as IEntityHealth;
            }
        }

        public IStunState Stun
        {
            get
            {
                if (!hasResolvedStun)
                {
                    if (UnityNull.IsNull(stunSource as IStunState))
                        stunSource = GetReferenceInParentOrChildren<IStunState>() as MonoBehaviour;
                    hasResolvedStun = true;
                }
                return stunSource as IStunState;
            }
        }

        public IGroundFitter Fitter => groundFitterSource as IGroundFitter;
        public Behaviour FitterBehaviour => groundFitterSource;

        #endregion

        public void Initialize()
        {
            if (attackModule)
                attackModule.SetBlackboard(this);
            if (detectionModule)
                detectionModule.SetBlackboard(this);
            if (movementModule)
                movementModule.SetBlackboard(this);
            if (aggroModule)
                aggroModule.SetBlackboard(this);
            if (rotationModule)
                rotationModule.Initialize(agent, Fitter);
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

        public IEntityStateModule GetStateModule()
        {
            return aggroModule;
        }

        #endregion

        #region Tactical Context

        public TacticalContext GetTacticalContext()
        {
            GameEntity target = aggroModule ? aggroModule.CurrentTarget : null;
            bool hasTarget = target != null;

            int targetAggroCount = 0;
            if (hasTarget && target.AggroTracker != null)
            {
                targetAggroCount = target.AggroTracker.AggroEntities.Count;
            }

            Vector3 selfPos = transform.position;
            Vector3 targetPos = hasTarget ? target.transform.position : selfPos;
            float distance = hasTarget ? Vector3.Distance(selfPos, targetPos) : 0f;

            float ownHealth = NormalizedHealth(Health);
            float targetHealth = hasTarget && target.TryGetComponent<IEntityHealth>(out var targetHp)
                ? NormalizedHealth(targetHp)
                : 1f;

            float threatScore = hasTarget && detectionModule != null
                ? detectionModule.GetThreatScore(target)
                : 0f;

            float timeSinceAttack = attackModule != null
                ? attackModule.TimeSinceLastAttack
                : 0f;

            bool hasLoS = hasTarget && detectionModule != null
                && detectionModule.HasLineOfSight(selfPos, targetPos);

            return new TacticalContext
            {
                HasTarget = hasTarget,
                Distance = distance,
                OwnHealthNormalized = ownHealth,
                TargetHealthNormalized = targetHealth,
                ThreatScore = threatScore,
                CanStartAttack = attackModule != null && attackModule.CanStartAttack(),
                HasLineOfSight = hasLoS,
                TimeSinceLastAttack = timeSinceAttack,
                TargetAggroCount = targetAggroCount,
                Aggressiveness = aggroModule != null && aggroModule.configuration != null ? aggroModule.configuration.aggressiveness : 0.5f
            };
        }

        private static float NormalizedHealth(IEntityHealth health)
        {
            if (UnityNull.IsNull(health) || health.MaxHealth <= 0f)
                return 1f;

            return Mathf.Clamp01(health.Health / health.MaxHealth);
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
