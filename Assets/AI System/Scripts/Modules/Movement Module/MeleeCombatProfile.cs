using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI
{
    public enum MeleePositioning { Front, Flank, Unrestricted }
    public enum MeleeRecoveryMode { MaintainDistance, HoldGround }

    [CreateAssetMenu(fileName = "Melee Combat Profile", menuName = "AI/Combat/Melee Combat Profile")]
    public sealed class MeleeCombatProfile : ScriptableObject
    {
        [Title("Positioning")]
        [Tooltip("Front: hedefin ön bölgesinde kalır. Flank: hedefin yan/arka bölgesini tercih eder. Unrestricted: açı kısıtlaması yoktur. Bölge ulaşılamazsa Advanced > Blocked Sector Grace sonrası alternatif açı kullanılabilir.")]
        public MeleePositioning positioning = MeleePositioning.Front;
        [ShowIf(nameof(IsFront)), Range(20f, 85f), Tooltip("Hedefin ileri yönünün her iki yanında izin verilen açı. 50 = toplam 100 derecelik ön bölge. Küçültmek daha dar bir ön alanda konumlanmayı sağlar; bekleme süresini değiştirmez.")]
        public float frontHalfAngle = 70f;
        [ShowIf(nameof(IsFlank)), Range(90f, 180f), Tooltip("Flank modunda hedefin ileri yönüne göre tercih edilen açı. 90 = yan, 180 = arka. Bu açının yaklaşık 20 derece çevresinde hareket eder.")]
        public float flankAngle = 120f;

        [Title("Spacing")]
        [HideIf(nameof(HoldsGround)), Min(0.1f), Tooltip("Maintain Distance modunda geri çekilme/circle sırasında hedefin yatay gövde sınırı ile robotun kök noktası arasındaki mesafe (metre). Saldırı erişimi veya agent yarıçapı gerektirirse otomatik büyütülür. Hold Ground beklemesinde kullanılmaz.")]
        public float holdingDistance = 4.5f;

        [Title("Recovery")]
        [Tooltip("Maintain Distance: oyuncu yaklaştıkça mesafeyi yeniden açar. Hold Ground: saldırı sonrası sabit bir kısa geri adım atıp bulunduğu bölgeyi korur; oyuncu yaklaşınca kaçmaz. Geri adım Movement Profile > Withdrawal Step Distance, yana adımlar Circling Steps ile ayarlanır.")]
        public MeleeRecoveryMode recoveryMode;
        [Min(0f), Tooltip("Geri çekilmeden sonraki Circle/bekleme aşamasının saniyesi. Adımlar arasındaki bekleme buna dahildir; cooldown beklemeyi uzatabilir. Hold Ground'da oyuncu en az 0.5 m yaklaşırsa hazır ve menzildeki saldırı ile erken karşılık verebilir.")]
        public float circleDuration = 1.5f;
        [Min(0f), Tooltip("Her Circle aşamasında toplam süreye 0 ile bu değer arasında rastgele saniye ekler. Örn. Duration 3 + Variance 0.5 = 3–3.5 saniye. Adımlar arasındaki beklemeyi ayarlamaz.")]
        public float circleDurationVariance = 0.5f;

        [FoldoutGroup("Advanced"), Min(0f), Tooltip("Holding Distance'a ulaşsa bile geri çekilme aşamasında geçirilecek minimum saniye. Bu süre boyunca mutlaka hareket edeceği anlamına gelmez.")]
        public float minimumWithdrawalTime = 0.6f;
        [FoldoutGroup("Advanced"), Min(0.1f), Tooltip("Geri çekilme için izin verilen maksimum saniye. Mesafeye ulaşamasa veya yol kapalı olsa bile süre dolunca Circle aşamasına geçer.")]
        public float withdrawalTimeout = 2.5f;
        [FoldoutGroup("Advanced"), Min(0.1f), Tooltip("Tercih edilen bölgeye ulaşılabilir konum bulunamazsa veya açı düzeltmesinde ilerleme yoksa alternatif açıya izin vermeden önce beklenecek saniye. Saldırı menzili ve hedefe bakma şartı korunur.")]
        public float blockedSectorGrace = 2f;
        [FoldoutGroup("Advanced"), Range(5f, 90f), Tooltip("Saldırı başlayabilmesi için robotun ileri yönü ile hedef yönü arasındaki maksimum açı. Küçük değer daha düzgün hedefe dönmeyi gerektirir. Front Half Angle farklıdır: o, hedef çevresindeki konumu sınırlar.")]
        public float attackFacingHalfAngle = 45f;

        private bool IsFront => positioning == MeleePositioning.Front;
        private bool IsFlank => positioning == MeleePositioning.Flank;
        private bool HoldsGround => recoveryMode == MeleeRecoveryMode.HoldGround;
    }
}
