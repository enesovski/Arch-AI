using Artifika.AI.Attack;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class RangedAttackModule : AttackSubModule
    {
        public override void Attack(BaseAttackDefinition def)
        {
            AttackModule.AttackData attackData = attackModule.FindAttackData(def);

            PerformAttack(def as RangedAttackDefinition, attackData.attackPoint, attackData.attackPerformFeedback);
        }

        public void PerformAttack(RangedAttackDefinition rangedDef, Transform firePoint, MMF_Player feedbackPlayer)
        {
            if (rangedDef == null)
                return;

            if (attackModule == null)
                return;

            if (!attackModule.TryGetCurrentTarget(out GameEntity threat))
                return;

            if (threat == null)
                return;

            // Cover may have moved since selection. Recheck at the animation event.
            if (!attackModule.IsRangedShotClear(rangedDef, threat, true))
                return;

            feedbackPlayer?.PlayFeedbacks();

            Vector3 targetPos = threat.AimPosition;

            Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
            Vector3 aimDirection = targetPos - spawnPos;
            Quaternion baseRot = aimDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(aimDirection.normalized)
                : transform.rotation;

            for (int i = 0; i < rangedDef.ProjectileCount; i++)
            {
                if (rangedDef.projectileMovementType == ProjectileMovementType.Linear)
                {
                    float halfAngle = rangedDef.SpreadAngle * 0.5f;
                    float yawOffset = Random.Range(-halfAngle, halfAngle);
                    Quaternion spreadRotation = Quaternion.Euler(0f, yawOffset, 0f) * baseRot;

                    GameObject projectileObject = Instantiate(rangedDef.ProjectilePrefab, spawnPos, spreadRotation);

                    if (projectileObject.TryGetComponent<ProjectileBase>(out var proj))
                    {
                        proj.Initialize(targetPos, selfNetworkObject);
                        proj.GetComponent<NetworkObject>().Spawn();
                    }
                }
                else if (rangedDef.projectileMovementType == ProjectileMovementType.Parabolic)
                {
                    Vector2 rnd = Random.insideUnitCircle * rangedDef.SpreadRadius;
                    Vector3 randomDest = targetPos + new Vector3(rnd.x, 0f, rnd.y);

                    GameObject go = Instantiate(rangedDef.ProjectilePrefab, spawnPos, baseRot);
                    if (go.TryGetComponent<ProjectileBase>(out var proj))
                    {
                        proj.GetComponent<NetworkObject>().Spawn();
                        proj.Initialize(randomDest, selfNetworkObject);
                    }
                }
                else if (rangedDef.projectileMovementType == ProjectileMovementType.Static)
                {
                    GameObject go = Instantiate(rangedDef.ProjectilePrefab, spawnPos, baseRot);
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
        }
    }
}
