using Artifika.AI.Attack;
using UnityEngine;
using MoreMountains.Feedbacks;
using Unity.Netcode;

[RequireComponent(typeof(AggroModule))]
public class RangedAttackModule : AttackSubModule
{
    [SerializeField]
    private AggroModule aggroModule;

    public override void Attack(BaseAttackDefinition def)
    {
        AttackModule.AttackData attackData = attackModule.FindAttackData(def);

        attackData.attackPerformFeedback?.PlayFeedbacks();
        PerformAttack(def as RangedAttackDefinition, attackData.attackPoint, attackData.attackPerformFeedback);
    }

    public void PerformAttack(RangedAttackDefinition rangedDef, Transform firePoint, MMF_Player feedbackPlayer)
    {
        GameEntity threat = aggroModule.CurrentTarget;
        if (threat == null) return;

        Vector3 targetPos = threat.transform.position;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        Quaternion baseRot = Quaternion.LookRotation((targetPos - spawnPos).normalized);

        for (int i = 0; i < rangedDef.ProjectileCount; i++)
        {
            if (rangedDef.projectileMovementType == ProjectileMovementType.Linear) //Linear
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
            else if (rangedDef.projectileMovementType == ProjectileMovementType.Parabolic) // Parabolic
            {
                Vector2 rnd = Random.insideUnitCircle * rangedDef.SpreadRadius;
                Vector3 randomDest = targetPos + new Vector3(rnd.x, 0f, rnd.y);

                GameObject go = Instantiate(rangedDef.ProjectilePrefab, spawnPos, baseRot);
                if (go.TryGetComponent<ProjectileBase>(out var proj))
                {
                    proj.GetComponent<NetworkObject>().Spawn();
                    proj.Initialize(randomDest,selfNetworkObject);

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
