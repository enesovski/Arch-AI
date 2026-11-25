using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Archura.Attack
{
    public partial class AttackModule : UpdatableEntityModule
    {
        private const float AttackEndGrace = 0.05f;

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
        [SerializeField] private CombatConfiguration combatConfiguration;
        [SerializeField] private float utilityThreshold = 0.1f;
        [SerializeField] private List<AttackData> attackDefinitions;

        private Dictionary<BaseAttackDefinition, float> cooldowns;

        private IEntityStateModule stateModule;
        private AnimatorModule animatorModule;
        private MovementModule movementModule;
        private RotationModule rotationModule;
        private IEntityHealth healthComponent;
        private IAttackAnimationEventSource eventHandler;

        private GameEntity target;

        private RotationMode previousRotationMode;
        private Coroutine currentAttackCoroutine;
        private BaseAttackDefinition currentAttackDefinition;
        private bool hasRotationOverride;

        private int selectionFrame = -1;
        private BaseAttackDefinition selectedDefinition;

        public bool CanAttack { get; private set; } = true;
        public bool IsAttackCommitted { get; private set; } = false;
        public float TimeSinceLastAttack { get; private set; } = 0f;

        public event Action OnAttackCommittedStarted;
        public event Action<BaseAttackDefinition> OnAttackPerformed;
        public event Action OnAttackCommittedEnded;

        protected override void OnInitialize()
        {
            healthComponent = blackboard.Health;
            eventHandler = GetComponentInChildren<IAttackAnimationEventSource>();

            stateModule = blackboard.GetStateModule();
            animatorModule = blackboard.animatorModule;
            movementModule = blackboard.movementModule;
            rotationModule = blackboard.rotationModule;

            cooldowns = new Dictionary<BaseAttackDefinition, float>();
            foreach (AttackData attack in attackDefinitions)
            {
                if (attack.attackDefinition != null && !cooldowns.ContainsKey(attack.attackDefinition))
                {
                    cooldowns.Add(attack.attackDefinition, 0f);
                }
            }

            if (stateModule != null)
            {
                target = stateModule.CurrentTarget;
                stateModule.OnTargetChanged += HandleTargetChanged;
            }

            if (eventHandler != null)
            {
                eventHandler.AttackAnimationTriggered += PerformAttack;
            }

            if (healthComponent != null)
            {
                healthComponent.OnServerDeath += HandleServerDeath;
            }
        }

        public override void PerformUpdate(float deltaTime)
        {
            TimeSinceLastAttack += deltaTime;

            foreach (var def in attackDefinitions)
            {
                if (!def.attackDefinition)
                    continue;

                if (cooldowns.TryGetValue(def.attackDefinition, out float currentCooldown))
                {
                    cooldowns[def.attackDefinition] = Mathf.Max(0f, currentCooldown - deltaTime);
                }
            }

            InvalidateSelection();
        }

        public bool CanStartAttack()
        {
            if (!CanAttack)
                return false;

            if (currentAttackCoroutine != null || IsAttackCommitted)
                return false;

            if (healthComponent != null && healthComponent.Health <= 0f)
                return false;

            if (!TryGetTargetDistance(out _))
                return false;

            return GetSelectedDefinition() != null;
        }

        public bool ShouldUseAttackBranch()
        {
            if (IsAttackCommitted)
                return true;

            return CanStartAttack();
        }

        public bool Attack()
        {
            if (currentAttackCoroutine != null || IsAttackCommitted)
                return true;

            if (!CanAttack || (healthComponent != null && healthComponent.Health <= 0f))
                return false;

            if (!TryGetTargetDistance(out _))
                return false;

            BaseAttackDefinition def = GetSelectedDefinition();

            if (def == null)
                return false;

            currentAttackCoroutine = StartCoroutine(PerformAttackSequence(def));
            return true;
        }

        public void PerformAttack(BaseAttackDefinition def)
        {
            // Matching events may each hit; late or interrupted events must not deal damage.
            if (!IsEnabled || !IsAttackCommitted || !def || def != currentAttackDefinition ||
                (!UnityNull.IsNull(healthComponent) && !healthComponent.IsAlive))
                return;

            switch (def.AttackType)
            {
                case AttackType.Ranged:
                    rangedModule.Attack(def);
                    break;

                case AttackType.Melee:
                    meleeModule.Attack(def);
                    break;
            }

            OnAttackPerformed?.Invoke(def);
        }

        private IEnumerator PerformAttackSequence(BaseAttackDefinition def)
        {
            BeginAttack(def);

            yield return new WaitForSeconds(GetAttackDuration(def));

            EndAttack(def);

            currentAttackCoroutine = null;
        }

        private void BeginAttack(BaseAttackDefinition def)
        {
            InvalidateSelection();
            CanAttack = false;
            TimeSinceLastAttack = 0f;
            currentAttackDefinition = def;

            SetMovementStopped(def, true);
            BeginAttackRotation(def);

            animatorModule.SetAttackIndex(def.AnimationIndex);
            animatorModule.SetAttacking(true);

            IsAttackCommitted = true;
            if (def is RangedAttackDefinition && movementModule)
                movementModule.BeginRangedAttack(target);
            OnAttackCommittedStarted?.Invoke();
        }

        public void InterruptAttack() => InterruptCurrentAttack(false);

        public void InterruptAttackForDeath() => InterruptCurrentAttack(true);

        private void InterruptCurrentAttack(bool forDeath)
        {
            InvalidateSelection();
            ResetCombatMovement();
            if (currentAttackCoroutine != null)
            {
                StopCoroutine(currentAttackCoroutine);
                currentAttackCoroutine = null;
            }

            if (animatorModule)
                animatorModule.SetAttacking(false);
            if (forDeath)
            {
                if (animatorModule)
                    animatorModule.SetSpeedParam(0f);
                if (movementModule)
                    movementModule.Stop();
            }
            if (movementModule)
                movementModule.SetStopped(forDeath);

            EndAttackRotation();

            bool wasCommitted = IsAttackCommitted;

            IsAttackCommitted = false;
            CanAttack = !forDeath;
            currentAttackDefinition = null;

            if (wasCommitted)
                OnAttackCommittedEnded?.Invoke();
        }

        private void EndAttack(BaseAttackDefinition def)
        {
            animatorModule.SetAttacking(false);

            SetMovementStopped(def, false);
            RestorePostAttackLocomotion();

            EndAttackRotation();

            IsAttackCommitted = false;
            if (movementModule)
            {
                if (def is MeleeAttackDefinition)
                    movementModule.BeginMeleeWithdrawal(target);
                else if (def is RangedAttackDefinition)
                    movementModule.BeginRangedReposition(target);
            }
            OnAttackCommittedEnded?.Invoke();

            cooldowns[def] = def.Cooldown;
            CanAttack = true;
            InvalidateSelection();
            currentAttackDefinition = null;
        }

        private float GetAttackDuration(BaseAttackDefinition def)
        {
            if (def == null)
                return 0f;

            return Mathf.Max(0.1f, def.AttackDuration + AttackEndGrace);
        }

        private void SetMovementStopped(BaseAttackDefinition def, bool stopped)
        {
            if (def.StopDuringAttack && movementModule)
            {
                movementModule.SetStopped(stopped);
            }
        }

        private void RestorePostAttackLocomotion()
        {
            if (movementModule == null || animatorModule == null || target == null)
                return;

            if (healthComponent != null && !healthComponent.IsAlive)
                return;

            if (IsTargetWithinCloseAttackRange(0.5f))
                return;

            MovementProfile profile = movementModule.MovementProfile;
            if (profile != null && profile.runMovementSpeed > 0f)
            {
                movementModule.SetSpeed(profile.runMovementSpeed);
            }
        }

        private void BeginAttackRotation(BaseAttackDefinition def)
        {
            if (rotationModule == null)
                return;

            switch (def.LookAtTargetMode)
            {
                case AttackLookAtTargetMode.None:
                    break;

                case AttackLookAtTargetMode.TrackTarget:
                    TrackTargetDuringAttack();
                    break;

                case AttackLookAtTargetMode.LockAtStart:
                    LockRotationAtAttackStart();
                    break;
            }

            rotationModule.SetRotationSpeed(720f);
        }

        private void TrackTargetDuringAttack()
        {
            if (rotationModule == null || target == null)
                return;

            HandleRotationMode();

            rotationModule.SetLookTarget(target.transform, target.AimOffset);
            rotationModule.SetMode(RotationMode.LookAt);
        }

        private void LockRotationAtAttackStart()
        {
            if (rotationModule == null)
                return;

            HandleRotationMode();

            rotationModule.ClearLookTarget();
            rotationModule.SetMode(RotationMode.None);
        }

        private void EndAttackRotation()
        {
            if (rotationModule == null || !hasRotationOverride)
                return;

            rotationModule.ClearLookTarget();
            rotationModule.SetMode(previousRotationMode);
            rotationModule.ResetRotationSpeed();

            hasRotationOverride = false;
        }

        private void HandleRotationMode()
        {
            if (hasRotationOverride)
                return;

            previousRotationMode = rotationModule.CurrentMode;
            hasRotationOverride = true;
        }

        #region Attack Selection

        public bool IsTargetWithinCombatRadius()
        {
            if (combatConfiguration == null)
                return false;

            return IsTargetWithinRadius(combatConfiguration.CombatRadius);
        }

        public bool IsTargetWithinCloseAttackRange(float rangePadding = 0f)
        {
            if (!target)
                return false;

            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                BaseAttackDefinition def = attackDefinitions[i].attackDefinition;

                if (def == null)
                    continue;

                if (def.MinRange > rangePadding)
                    continue;

                if (TryGetAttackDistance(i, out float distance) && distance <= GetMaximumReach(def) + rangePadding)
                    return true;
            }

            return false;
        }

        public bool IsTargetTooCloseForAttack(float rangePadding = 0f)
        {
            bool hasAttackDefinition = false;
            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                BaseAttackDefinition def = attackDefinitions[i].attackDefinition;

                if (def == null || !TryGetAttackDistance(i, out float distance))
                    continue;

                hasAttackDefinition = true;
                if (distance >= def.MinRange + rangePadding)
                    return false;
            }

            return hasAttackDefinition;
        }

        private bool IsTargetWithinRadius(float radius)
        {
            if (radius <= 0f)
                return false;

            if (!TryGetTargetDistance(out float distance))
                return false;

            return distance <= radius;
        }

        private bool TryGetTargetDistance(out float distance)
        {
            distance = 0f;

            if (!target)
                return false;

            Transform targetTransform = target.transform;

            if (targetTransform == null)
                return false;

            distance = Vector3.Distance(transform.position, targetTransform.position);
            return true;
        }

        // Several graph nodes ask for the same selection every frame; positions only change between frames.
        private BaseAttackDefinition GetSelectedDefinition()
        {
            if (selectionFrame != Time.frameCount)
            {
                selectedDefinition = SelectBestDefinition();
                selectionFrame = Time.frameCount;
            }

            return selectedDefinition;
        }

        private void InvalidateSelection()
        {
            selectionFrame = -1;
            selectedDefinition = null;
        }

        private BaseAttackDefinition SelectBestDefinition()
        {
            if (!TryGetTargetDistance(out _))
                return null;

            float bestScore = float.MinValue;
            BaseAttackDefinition bestDef = null;

            float healthPercentage = healthComponent != null && healthComponent.MaxHealth > 0f
                ? Mathf.Clamp01(healthComponent.Health / healthComponent.MaxHealth)
                : 1f;

            bool rangedReady = !UsesRangedPositioning || movementModule.IsRangedPositionReady(target);

            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                BaseAttackDefinition def = attackDefinitions[i].attackDefinition;

                if (def == null)
                    continue;

                if (cooldowns.TryGetValue(def, out float cooldown) && cooldown > 0f)
                    continue;

                // Reject recovery phases before querying collider distance.
                if (def is MeleeAttackDefinition && !IsMeleePositionReady())
                    continue;
                if (def is RangedAttackDefinition && !rangedReady)
                    continue;

                if (!TryGetAttackDistance(i, out float attackDistance))
                    continue;

                if (attackDistance < def.MinRange || attackDistance > GetMaximumReach(def))
                    continue;
                if (def is RangedAttackDefinition && UsesRangedPositioning && !HasRangedLineOfFire(i, target, Vector3.zero))
                    continue;

                float distancePercentage = Mathf.InverseLerp(def.MinRange, def.MaxRange, attackDistance);

                float score =
                    def.DistanceUtilityCurve.Evaluate(distancePercentage)
                    + def.HealthUtilityCurve.Evaluate(healthPercentage)
                    + def.AdditionalUtilityScore;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDef = def;
                }
            }

            return bestDef != null && bestScore > utilityThreshold ? bestDef : null;
        }

        public AttackData FindAttackData(BaseAttackDefinition def)
        {
            foreach (AttackData item in attackDefinitions)
            {
                if (item.attackDefinition == def)
                    return item;
            }

            return default;
        }

        #endregion

        private void HandleTargetChanged(TargetChangeEventArgs args)
        {
            ResetCombatMovement();
            target = args.NewTarget;
            InvalidateSelection();
        }

        private void ResetCombatMovement()
        {
            if (!movementModule)
                return;

            movementModule.ResetMeleeCombat();
            movementModule.ResetRangedCombat();
        }

        public bool TryGetCurrentTarget(out GameEntity currentTarget)
        {
            currentTarget = target;
            return currentTarget != null;
        }

        public override void DestroyModule()
        {
            ResetCombatMovement();
            if (healthComponent != null)
            {
                healthComponent.OnServerDeath -= HandleServerDeath;
            }

            if (stateModule != null)
            {
                stateModule.OnTargetChanged -= HandleTargetChanged;
            }

            if (eventHandler != null)
            {
                eventHandler.AttackAnimationTriggered -= PerformAttack;
            }
        }

        private void HandleServerDeath()
        {
            InterruptAttackForDeath();
        }
    }
}
