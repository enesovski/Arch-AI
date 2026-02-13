using Artifika.AI.Aggro.Reactions;
using Artifika.AI.Attack;
using Artifika.AI.Death.Reactions;
using Sirenix.OdinInspector;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace Artifika.AI
{
    public class Blackboard : MonoBehaviour
    {
        #region Inspector Properties
        
        [Title("Modules")]
        public AttackModule attackModule;
        public DetectionModule detectionModule;
        public MovementModule movementModule;
        public AggroModule aggroModule;
        public AnimatorModule animatorModule;
        public RotationModule rotationModule;
        
        [Title("Components")]
        public GameEntity gameEntity;
        public BehaviorGraphAgent behaviorGraphAgent;
        public HealthComponent healthComponent;
        public AIGroundFitter fitter;
        public NavMeshAgent agent;
        public Animator animator;

        [Title("Busses")]
        public AggroReactionsBus aggroReactionsBus;
        public DeathReactionsBus deathReactionsBus;
        
        [Title("Configuration")]
        public float nestRadius = 40f;
        public float idleTime = 3f;
        
        #endregion

        public void Initialize()
        {
            attackModule?.SetBlackboard(this);
            detectionModule?.SetBlackboard(this);
            movementModule?.SetBlackboard(this);
            aggroModule?.SetBlackboard(this);
            rotationModule?.Initialize(agent, fitter);
            
            deathReactionsBus.Initialize(this);
            aggroReactionsBus.Initialize(this);
        }

        #region Properties

        public bool HasDetected(GameEntity sourceEntity)
        {
            return detectionModule && detectionModule.HasDetected(sourceEntity);
        }

        #endregion
        
    }
}