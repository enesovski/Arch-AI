using Sirenix.OdinInspector;
using UnityEngine;

namespace Archura.Attack
{
    [CreateAssetMenu(fileName = "MeleeAttackDefinition", menuName = "AI/Attacks/MeleeAttackDefinition")]
    public class MeleeAttackDefinition : BaseAttackDefinition
    {
        [Title("Damage")]
        [Tooltip("Host-defined damage data for this attack (damage types, stun, etc.). " +
                 "The AI passes it through untouched in MeleeHit.Definition; the host's IAttackReceiver interprets it.")]
        [SerializeField] private ScriptableObject damageConfig;

        [Title("Variance")]
        [SerializeField] private float damageVariancePercent = 0.2f;

        [Title("Attack Detection")]
        [Tooltip("Layers this hitbox can damage.")]
        [SerializeField] private LayerMask attackableLayers;
        [SerializeField] private float attackRadius = 1.5f;
        [SerializeField] private float attackRange = 1.5f;

        [Title("Player Knockback")]
        [SerializeField, Tooltip("Knocks the target back when the hit deals damage. The AI computes the impulse on the server; the target's IAttackReceiver decides whether and how to apply it.")]
        private bool knockbackEnabled;
        [SerializeField, ShowIf(nameof(knockbackEnabled)), Range(0f, 20f)]
        [Tooltip("Initial speed away from the attacker (m/s). Distance is roughly speed² / (2 × Deceleration); 8 speed with 14 deceleration pushes about 2.3 m on flat ground.")]
        private float knockbackHorizontalSpeed = 8f;
        [SerializeField, ShowIf(nameof(knockbackEnabled)), Range(0f, 12f)]
        [Tooltip("Initial upward speed (m/s). Small values give a short hop; high values may cause fall damage on landing.")]
        private float knockbackUpwardSpeed = 7f;
        [SerializeField, ShowIf(nameof(knockbackEnabled)), Range(1f, 80f)]
        [Tooltip("How fast the horizontal knockback speed decays (m/s²). Higher values give a shorter push; gravity handles vertical motion.")]
        private float knockbackDeceleration = 14f;

        public ScriptableObject DamageConfig => damageConfig;
        public float DamageVariancePercent => damageVariancePercent;
        public LayerMask AttackableLayers => attackableLayers;
        public float AttackRadius => attackRadius;
        public float AttackRange => attackRange;
        public bool KnockbackEnabled => knockbackEnabled;
        public float KnockbackHorizontalSpeed => Mathf.Clamp(knockbackHorizontalSpeed, 0f, 20f);
        public float KnockbackUpwardSpeed => Mathf.Clamp(knockbackUpwardSpeed, 0f, 12f);
        public float KnockbackDeceleration => Mathf.Clamp(knockbackDeceleration, 1f, 80f);
    }
}
