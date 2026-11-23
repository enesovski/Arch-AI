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
        [SerializeField, Tooltip("İsabet edip hasar verdiğinde oyuncuyu savurur. Kararı server verir; oyuncunun hareket motoru uygular. Ölü/yaralı, binen, tırmanan veya derin suda yüzen oyuncular savrulmaz.")]
        private bool knockbackEnabled;
        [SerializeField, ShowIf(nameof(knockbackEnabled)), Range(0f, 20f)]
        [Tooltip("Saldırandan dışarı doğru başlangıç hızı (m/s). Mesafe yaklaşık hız² / (2 × Deceleration). 8 hız ve 14 yavaşlama, düz zeminde yaklaşık 2.3 m itiş üretir.")]
        private float knockbackHorizontalSpeed = 8f;
        [SerializeField, ShowIf(nameof(knockbackEnabled)), Range(0f, 12f)]
        [Tooltip("Yukarı başlangıç hızı (m/s). 7, mevcut oyuncu yerçekimiyle küçük bir havalanma üretir. Yüksek değerler inişte düşme hasarı verebilir. Zıplama tuşundan etkilenmez.")]
        private float knockbackUpwardSpeed = 7f;
        [SerializeField, ShowIf(nameof(knockbackEnabled)), Range(1f, 80f)]
        [Tooltip("Yatay savrulma hızının saniyede ne kadar azalacağı (m/s²). Yüksek değer daha kısa savrulma üretir. Dikey hareketi yerçekimi yönetir.")]
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
