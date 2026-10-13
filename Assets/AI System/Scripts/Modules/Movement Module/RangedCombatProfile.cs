using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI
{
    [CreateAssetMenu(fileName = "Ranged Combat Profile", menuName = "AI/Combat/Ranged Combat Profile")]
    public sealed class RangedCombatProfile : ScriptableObject
    {
        [MinMaxSlider(1f, 50f, true)]
        [Tooltip("Hedefin kök noktası ile robot arasındaki tercih edilen yatay mesafe (metre). Bu bir hasar menzili değildir; silahın gerçek menzili ayrıca kontrol edilir. Uygun konum bulunamazsa daha yakın bir atış yapılabilir.")]
        public Vector2 preferredDistanceRange = new(12f, 20f);

        [Min(0f), Tooltip("Oyuncu bu yatay mesafeden yakına girince kısa bir kaçış adımı başlatır. Hedef adım sırasında uzatılmaz. Mesafeye ulaşamazsa kısa süre konumunu koruyup uygun yakın saldırıyla karşılık verebilir.")]
        public float retreatTriggerDistance = 8f;

        [Min(0f), Tooltip("Konum değişiminden sonra ateş etmeden önce durup nişan aldığı saniye. Silah cooldown'u beklemeyi uzatabilir. Atış animasyonu sırasında hareket edilmez.")]
        public float aimPause = 0.35f;

        [Min(0.2f), Tooltip("Bir yana/geri adım için izin verilen maksimum saniye. Yol kapalıysa veya ilerleyemiyorsa süre sonunda hareket zorunluluğu kalkar; açık atış varsa bulunduğu yerden ateş edebilir. Açık atış yoksa yeni konum aramayı sürdürür.")]
        public float repositionTimeout = 2.5f;

        [FoldoutGroup("Advanced")]
        [Tooltip("Muzzle ile hedefin nişan noktası arasında atışı engelleyen collider katmanları. Terrain, Ground, Structure ve çevre duvarlarını dahil edin. Tetikleyiciler, robotun kendisi ve hedefin collider'ları otomatik yok sayılır. Görüş sensörü maskesinden bağımsızdır.")]
        public LayerMask shootingObstacleMask = Physics.DefaultRaycastLayers;

        public float MinimumDistance => Mathf.Max(1f, Mathf.Min(preferredDistanceRange.x, preferredDistanceRange.y));
        public float MaximumDistance => Mathf.Max(MinimumDistance, Mathf.Max(preferredDistanceRange.x, preferredDistanceRange.y));
    }
}
