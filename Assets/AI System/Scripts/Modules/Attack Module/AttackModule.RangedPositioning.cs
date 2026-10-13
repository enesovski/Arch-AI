using System;
using UnityEngine;

namespace Artifika.AI.Attack
{
    public partial class AttackModule
    {
        internal bool HasRangedAttackModule => rangedModule;
        private RaycastHit[] rangedSightHits = new RaycastHit[8];
        private Collider[] rangedMuzzleOverlaps = new Collider[8];
        private RangedSightSample[] rangedSightSamples;
        private struct RangedSightSample
        {
            public GameEntity target;
            public Vector3 origin;
            public Vector3 aim;
            public float until;
            public int mask;
            public bool clear;
        }

        internal bool UsesRangedPositioning => movementModule && movementModule.UsesRangedCombatMovement;

        internal bool IsRangedShotClear(RangedAttackDefinition def, GameEntity shotTarget, bool refresh = false)
        {
            if (!UsesRangedPositioning)
                return true;

            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                if (attackDefinitions[i].attackDefinition == def)
                    return HasRangedLineOfFire(i, shotTarget, Vector3.zero, refresh);
            }
            return false;
        }

        internal bool HasRangedShotAt(Vector3 rootPosition, GameEntity shotTarget)
        {
            if (!shotTarget) return false;
            Vector3 offset = rootPosition - transform.position;
            bool queried = false;
            Vector3 lastOrigin = default;
            for (int i = 0; i < attackDefinitions.Count; i++)
            {
                if (!(attackDefinitions[i].attackDefinition is RangedAttackDefinition def)) continue;
                Vector3 origin = RangedOriginAt(i, shotTarget, offset);
                float distance = Vector3.Distance(origin, shotTarget.AimPosition);
                // Cooldowns intentionally do not exclude otherwise useful firing positions.
                if (distance < def.MinRange || distance > def.MaxRange) continue;
                // Weapons sharing a muzzle can reuse a blocked candidate query.
                if (queried && (origin - lastOrigin).sqrMagnitude < 0.0001f) continue;
                queried = true;
                lastOrigin = origin;
                if (HasRangedLineOfFire(i, shotTarget, offset)) return true;
            }
            return false;
        }

        private Vector3 RangedOrigin(int index) => attackDefinitions[index].attackPoint
            ? attackDefinitions[index].attackPoint.position : transform.position;

        private Vector3 RangedOriginAt(int index, GameEntity shotTarget, Vector3 offset)
        {
            Vector3 origin = RangedOrigin(index);
            if (offset.sqrMagnitude < 0.0001f) return origin;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            Vector3 facing = shotTarget.AimPosition - (transform.position + offset);
            facing.y = 0f;
            if (forward.sqrMagnitude < 0.001f || facing.sqrMagnitude < 0.001f) return origin + offset;
            // Predict muzzle yaw at the candidate while facing the target.
            return transform.position + offset + Quaternion.Euler(0f, Vector3.SignedAngle(forward, facing, Vector3.up), 0f) * (origin - transform.position);
        }

        private bool HasRangedLineOfFire(int index, GameEntity shotTarget, Vector3 offset, bool refresh = false)
        {
            if (!shotTarget) return false;
            Vector3 origin = RangedOriginAt(index, shotTarget, offset);
            Vector3 aim = shotTarget.AimPosition;
            int mask = movementModule.RangedCombatProfile.shootingObstacleMask.value;

            // Only the real muzzle query is cached. Candidate queries run on bounded replans.
            bool cache = offset.sqrMagnitude < 0.0001f;
            if (cache && (rangedSightSamples == null || rangedSightSamples.Length != attackDefinitions.Count))
                rangedSightSamples = new RangedSightSample[attackDefinitions.Count];
            if (cache && !refresh)
            {
                for (int i = 0; i < rangedSightSamples.Length; i++)
                {
                    RangedSightSample sample = rangedSightSamples[i];
                    if (sample.target == shotTarget && sample.until > Time.time && sample.mask == mask &&
                        (sample.origin - origin).sqrMagnitude < 0.04f && (sample.aim - aim).sqrMagnitude < 0.04f)
                        return sample.clear;
                }
            }

            Vector3 ray = aim - origin;
            float length = ray.magnitude;
            bool clear = true;
            if (length > 0.05f)
            {
                int count;
                do
                {
                    count = Physics.RaycastNonAlloc(origin, ray / length, rangedSightHits,
                        length - 0.02f, mask, QueryTriggerInteraction.Ignore);
                    if (count == rangedSightHits.Length) Array.Resize(ref rangedSightHits, rangedSightHits.Length * 2);
                    else break;
                } while (true);
                for (int i = 0; i < count; i++)
                {
                    Transform hit = rangedSightHits[i].collider.transform;
                    if (hit.IsChildOf(transform) || hit.IsChildOf(shotTarget.transform)) continue;
                    clear = false;
                    break;
                }
            }
            // Raycasts miss solids containing their origin; check the muzzle volume too.
            if (clear)
            {
                int count;
                do
                {
                    count = Physics.OverlapSphereNonAlloc(origin, 0.05f, rangedMuzzleOverlaps, mask, QueryTriggerInteraction.Ignore);
                    if (count == rangedMuzzleOverlaps.Length) Array.Resize(ref rangedMuzzleOverlaps, rangedMuzzleOverlaps.Length * 2);
                    else break;
                } while (true);
                for (int i = 0; i < count; i++)
                {
                    Transform hit = rangedMuzzleOverlaps[i].transform;
                    if (hit.IsChildOf(transform) || hit.IsChildOf(shotTarget.transform)) continue;
                    clear = false;
                    break;
                }
            }
            if (cache)
                rangedSightSamples[index] = new RangedSightSample
                {
                    target = shotTarget,
                    origin = origin,
                    aim = aim,
                    mask = mask,
                    clear = clear,
                    until = Time.time + 0.15f
                };
            return clear;
        }
    }
}
