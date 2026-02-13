using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Attack
{
    [CreateAssetMenu(fileName = "MeleeAttackDefinition", menuName = "AI/Attacks/MeleeAttackDefinition")]
    public class MeleeAttackDefinition : BaseAttackDefinition
    {
        [Title("Damage Stats")]
        [SerializeField] private float physicalDamage = 20f;
        [SerializeField] private float piercingDamage = 0f;
        [SerializeField] private float elysioriteDamage = 0f;
        [SerializeField] private float damageInterval = 0.2f;

        [Title("Attack Detection")]
        [Tooltip("Layers this hitbox can damage.")]
        [SerializeField] private LayerMask attackableLayers;
        [SerializeField] private float attackRadius = 1.5f;
        [SerializeField] private float attackRange = 1.5f;

        public float PhysicalDamage => physicalDamage;
        public float PiercingDamage => piercingDamage;
        public float ElysioriteDamage => elysioriteDamage;
        public float DamageInterval => damageInterval;
        public LayerMask AttackableLayers => attackableLayers;
        public float AttackRadius => attackRadius;
        public float AttackRange => attackRange;

    }
}