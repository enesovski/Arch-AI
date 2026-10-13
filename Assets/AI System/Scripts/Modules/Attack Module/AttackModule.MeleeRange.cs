using UnityEngine;

namespace Artifika.AI.Attack
{
    public partial class AttackModule
    {
        private struct RangeSample
        {
            public int frame;
            public GameEntity target;
            public bool valid;
            public float distance;
        }
        private RangeSample[] attackRangeSamples;

        internal bool HasMeleeAttackModule => meleeModule;
        internal bool CurrentAttackStopsMovement => currentAttackDefinition && currentAttackDefinition.StopDuringAttack;
        internal int MeleeLayerMask => meleeModule ? meleeModule.AttackableLayerMask : ~0;

        private static float GetMaximumReach(BaseAttackDefinition def)
        {
            return def is MeleeAttackDefinition melee
                ? Mathf.Min(def.MaxRange, melee.AttackRange + melee.AttackRadius)
                : def.MaxRange;
        }
        private bool TryGetAttackDistance(int index, out float distance)
        {
            distance = 0f;
            if (!target)
                return false;

            AttackData attack = attackDefinitions[index];
            if (attack.attackDefinition is RangedAttackDefinition && UsesRangedPositioning)
            {
                distance = Vector3.Distance(RangedOrigin(index), target.AimPosition);
                return true;
            }
            if (!(attack.attackDefinition is MeleeAttackDefinition))
                return TryGetTargetDistance(out distance);

            if (attackRangeSamples == null || attackRangeSamples.Length != attackDefinitions.Count)
                attackRangeSamples = new RangeSample[attackDefinitions.Count];

            RangeSample sample = attackRangeSamples[index];
            if (sample.frame != Time.frameCount || sample.target != target)
            {
                Vector3 origin = attack.attackPoint ? attack.attackPoint.position : transform.position;
                sample.frame = Time.frameCount;
                sample.target = target;
                sample.valid = target.TryGetClosestCombatPoint(origin, MeleeLayerMask, out Vector3 point);
                sample.distance = sample.valid ? Vector3.Distance(origin, point) : 0f;
                attackRangeSamples[index] = sample;
            }
            distance = sample.distance;
            return sample.valid;
        }

        private bool IsMeleePositionReady()
        {
            return !movementModule || movementModule.IsMeleePositionReady(target);
        }

        public override void OnModuleDisabled()
        {
            movementModule?.ResetMeleeCombat();
            movementModule?.ResetRangedCombat();
            base.OnModuleDisabled();
        }

        internal bool HasReadyMeleeDefinition()
        {
            foreach (AttackData attack in attackDefinitions)
            {
                if (attack.attackDefinition is MeleeAttackDefinition def &&
                    (!cooldowns.TryGetValue(def, out float remaining) || remaining <= 0f))
                    return true;
            }
            return false;
        }

        internal bool HasReadyMeleeAttackInRange()
        {
            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                if (!(attackDefinitions[i].attackDefinition is MeleeAttackDefinition def) ||
                    cooldowns.TryGetValue(def, out float remaining) && remaining > 0f ||
                    !TryGetAttackDistance(i, out float distance))
                    continue;

                if (distance >= def.MinRange && distance <= GetMaximumReach(def))
                    return true;
            }
            return false;
        }

        internal float GetApproachClearance(float agentRadius, float rangeFraction)
        {
            float clearance = float.PositiveInfinity;
            Vector3 forward = FlatDirection(transform.forward);
            bool hasReadyAttack = HasReadyMeleeDefinition();
            foreach (AttackData attack in attackDefinitions)
            {
                if (!(attack.attackDefinition is MeleeAttackDefinition def))
                    continue;
                if (hasReadyAttack && cooldowns.TryGetValue(def, out float remaining) && remaining > 0f)
                    continue;

                float pointOffset = attack.attackPoint
                    ? Vector3.Dot(attack.attackPoint.position - transform.position, forward) : 0f;
                float desiredRange = Mathf.Lerp(def.MinRange, GetMaximumReach(def), Mathf.Clamp(rangeFraction, 0.5f, 0.95f));
                clearance = Mathf.Min(clearance, Mathf.Max(0.2f, desiredRange + pointOffset));
            }
            return float.IsPositiveInfinity(clearance) ? agentRadius + 0.5f : clearance;
        }

        internal float GetReadyMeleeRangeCorrection(float rangeFraction)
        {
            float correction = float.PositiveInfinity;
            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                if (!(attackDefinitions[i].attackDefinition is MeleeAttackDefinition def) ||
                    cooldowns.TryGetValue(def, out float remaining) && remaining > 0f ||
                    !TryGetAttackDistance(i, out float distance))
                    continue;

                float maximum = GetMaximumReach(def);
                // Wait for facing without closing an already valid attack range.
                if (distance >= def.MinRange && distance <= maximum)
                    return 0f;
                float desired = Mathf.Lerp(def.MinRange, maximum, Mathf.Clamp(rangeFraction, 0.5f, 0.95f));
                float candidate = distance - desired;
                if (Mathf.Abs(candidate) < Mathf.Abs(correction))
                    correction = candidate;
            }
            return float.IsPositiveInfinity(correction) ? 0f : Mathf.Clamp(correction, -0.5f, 0.5f);
        }

        private static Vector3 FlatDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
