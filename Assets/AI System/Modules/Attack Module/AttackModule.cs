using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;
using System;

namespace Artifika.AI.Attack
{
    public class AttackModule : UpdatableEntityModule
    {
        [Serializable]
        public struct AttackData
        {
            [Title("Configuration")]
            [LabelText("Point")]
            public Transform attackPoint;
            [LabelText("Feedback")]
            public MMF_Player attackPerformFeedback;
            
            [Title("Attack")]
            [LabelText("Attack")]
            [InlineEditor] 
            public BaseAttackDefinition attackDefinition;
        }

        [Title("Submodules")]
        [SerializeField] private RangedAttackModule rangedModule;
        [SerializeField] private MeleeAttackModule meleeModule;

        [Title("Properties")]
        [SerializeField] private float utilityThreshold = 0.1f;
        [SerializeField]
        private List<AttackData> attackDefinitions;
        
        private Dictionary<BaseAttackDefinition, float> cooldowns;
    
        private AggroModule aggroModule;
        private AnimatorModule animatorModule;
        private MovementModule movementModule;
        private RotationModule rotationModule;
        private HealthComponent healthComponent;
        private AnimatorEventHandler eventHandler;
        
        private GameEntity target;

        public bool canAttack { get; private set; } = true;
        public bool IsAttackCommitted { get; private set; } = false;
        public event Action OnAttackCommittedStarted;
        public event Action OnAttackCommittedEnded;

        protected override void OnInitialize()
        {
            healthComponent = blackboard.healthComponent;
            eventHandler = GetComponentInChildren<AnimatorEventHandler>();

            aggroModule = blackboard.aggroModule;
            animatorModule = blackboard.animatorModule;
            movementModule = blackboard.movementModule;
            rotationModule = blackboard.rotationModule;

            cooldowns = attackDefinitions.ToDictionary(attack => attack.attackDefinition, def => 0f);

            aggroModule.OnTargetChanged += newTarget => target = newTarget.NewTarget;
            eventHandler.OnAttackAnimation += PerformAttack;
        }

        public override void PerformUpdate(float deltaTime)
        {
            foreach (var def in attackDefinitions)
            {
                if (def.attackDefinition == null)
                    continue;

                if (cooldowns.TryGetValue(def.attackDefinition, out float currentCooldown))
                {
                    cooldowns[def.attackDefinition] = Mathf.Max(0f, currentCooldown - deltaTime);
                }
            }
        }

        public bool CanAttack()
        {
            if (!canAttack)
                return false;

            if (!target || healthComponent == null || healthComponent.Health <= 0)
                return false;

            return SelectBestDefinition() != null;
        }

        public void Attack()
        {
            BaseAttackDefinition def = SelectBestDefinition();
            
            if (def)
                StartCoroutine(PerformAttackSequence(def));
        }

        public void PerformAttack(BaseAttackDefinition def)
        {
            switch (def.AttackType)
            {
                case AttackType.Ranged:
                    rangedModule.Attack(def);
                    break;
                case AttackType.Melee:
                    meleeModule.Attack(def);
                    break;
            }
        }

        private IEnumerator PerformAttackSequence(BaseAttackDefinition def)
        {
            if (!def)
                yield break;

            canAttack = false;
            
            if (def.StopDuringAttack && movementModule != null)
                movementModule.SetStopped(true);

            // --- Rotation: switch to LookAt if this attack definition requires it ---
            RotationMode previousMode = RotationMode.Agent;
            bool useLookAt = def.lookAtTargetDuringAttack && rotationModule != null && target != null;

            if (useLookAt)
            {
                previousMode = rotationModule.CurrentMode;
                rotationModule.SetLookTarget(target.transform);
                rotationModule.SetMode(RotationMode.LookAt);
            }

            IsAttackCommitted = true;
            OnAttackCommittedStarted?.Invoke();

            yield return new WaitForSeconds(def.TelegraphTime);

            animatorModule.SetAttackIndex(def.AnimationIndex);
            animatorModule.SetAttacking(true);

            yield return new WaitForSeconds(def.ExecuteTime);

            cooldowns[def] = def.Cooldown;
            yield return new WaitForSeconds(def.RecoveryTime);

            animatorModule.SetAttacking(false);
            
            if (def.StopDuringAttack && movementModule != null)
                movementModule.SetStopped(false);

            // --- Rotation: restore previous mode ---
            if (useLookAt)
            {
                rotationModule.ClearLookTarget();
                rotationModule.SetMode(previousMode);
            }

            IsAttackCommitted = false;
            OnAttackCommittedEnded?.Invoke();

            canAttack = true;
        }

        private BaseAttackDefinition SelectBestDefinition()
        {
            if (!target)
                return null;

            float bestScore = float.MinValue;
            BaseAttackDefinition bestDef = null;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            float healthPercentage = healthComponent.Health / healthComponent.MaxHealth;

            foreach (AttackData attack in attackDefinitions)
            {
                BaseAttackDefinition def = attack.attackDefinition;
                if (def == null)
                    continue;

                if (!cooldowns.TryGetValue(def, out float currentCooldown))
                    continue;

                if (currentCooldown > 0f)
                    continue;

                if (distance < def.MinRange || distance > def.MaxRange)
                    continue;

                float distancePercentage = Mathf.InverseLerp(def.MinRange, def.MaxRange, distance);
                float score = def.DistanceUtilityCurve.Evaluate(distancePercentage)
                              + def.HealthUtilityCurve.Evaluate(healthPercentage)
                              + UnityEngine.Random.Range(0f, def.RandomUtilityVariance)
                              + def.AdditionalUtilityScore;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDef = def;
                }
            }

            return (bestDef != null && bestScore > utilityThreshold) ? bestDef : null;
        }

        public AttackData FindAttackData(BaseAttackDefinition def)
        {
            AttackData found = default;
            foreach (AttackData item in attackDefinitions)
            {
                if (item.attackDefinition == def)
                {
                    found = item;
                    break;
                }
            }

            return found;
        }

        public override void DestroyModule()
        {
            eventHandler.OnAttackAnimation -= PerformAttack;
        }
    }
}