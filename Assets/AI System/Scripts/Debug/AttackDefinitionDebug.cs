using Artifika.AI.Attack;
using UnityEngine;

public class AttackDefinitionDebug : MonoBehaviour
{
    [SerializeField] Transform attackPoint;
    [SerializeField] MeleeAttackDefinition attackDef;

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;

        Vector3 pointA = attackPoint.position;
        Vector3 pointB = attackPoint.position + transform.forward * attackDef.AttackRange;

        Gizmos.DrawWireSphere(pointA, attackDef.AttackRadius);
        Gizmos.DrawWireSphere(pointB, attackDef.AttackRadius);

        Gizmos.DrawLine(pointA + Vector3.up * attackDef.AttackRadius, pointB + Vector3.up * attackDef.AttackRadius);
        Gizmos.DrawLine(pointA - Vector3.up * attackDef.AttackRadius, pointB - Vector3.up * attackDef.AttackRadius);
        Gizmos.DrawLine(pointA + transform.right * attackDef.AttackRadius, pointB + transform.right * attackDef.AttackRadius);
        Gizmos.DrawLine(pointA - transform.right * attackDef.AttackRadius, pointB - transform.right * attackDef.AttackRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + transform.forward * attackDef.AttackRange);
    }
}
