using Artifika.AI.Attack;
using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Artifika.AI
{
    [DisallowMultipleComponent]
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
        public HealthComponent healthComponent;
        public AIGroundFitter fitter;
        public NavMeshAgent agent;
        public Animator animator;
        public EntityStatsController entityStatsController;
        
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

        public void Initialize()
        {
            attackModule?.SetBlackboard(this);
            detectionModule?.SetBlackboard(this);
            movementModule?.SetBlackboard(this);
            aggroModule?.SetBlackboard(this);
            rotationModule?.Initialize(agent, fitter);
            fitter?.Initialize(this);

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
            if (healthComponent == null)
                healthComponent = GetReferenceInParentOrChildren<HealthComponent>();

            if (!healthComponent) return true;
            return healthComponent.IsAlive;
        }
        
        public bool IsStunned()
        {
            if (entityStatsController == null)
                entityStatsController = GetReferenceInParentOrChildren<EntityStatsController>();

            return entityStatsController != null && entityStatsController.IsStunned;
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

            float ownHealth = healthComponent != null
                ? Mathf.Clamp01(healthComponent.Health / healthComponent.MaxHealth)
                : 1f;

            float targetHealth = hasTarget && target.TryGetComponent<HealthComponent>(out var targetHp)
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
        
        
        private T GetReferenceInParentOrChildren<T>() where T : Component
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

            // Modules
            attackModule = GetReferenceInChildren<AttackModule>();
            detectionModule = GetReferenceInChildren<DetectionModule>();
            movementModule = GetReferenceInChildren<MovementModule>();
            aggroModule = GetReferenceInChildren<AggroModule>();
            animatorModule = GetReferenceInChildren<AnimatorModule>();
            rotationModule = GetReferenceInChildren<RotationModule>();

            // Components
            gameEntity = GetReferenceInParentOrChildren<GameEntity>();
            behaviorGraphAgent = GetReferenceInChildren<BehaviorGraphAgent>();
            healthComponent = GetReferenceInParentOrChildren<HealthComponent>();
            fitter = GetReferenceInChildren<AIGroundFitter>();
            agent = GetReferenceInChildren<NavMeshAgent>();
            animator = GetReferenceInChildren<Animator>();
            entityStatsController = GetReferenceInParentOrChildren<EntityStatsController>();

            companionStateModule = GetReferenceInChildren<CompanionStateModule>();
            commandModule = GetReferenceInChildren<CommandModule>();

            reactions = GetComponentsInChildren<ReactionBusBase>(true);

            EditorUtility.SetDirty(this);
        }

        private T GetReferenceInChildren<T>() where T : Component
        {
            return GetComponentInChildren<T>(true);
        }

#endif
    }
}
