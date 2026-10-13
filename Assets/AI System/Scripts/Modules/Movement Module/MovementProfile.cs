using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "Movement Profile", menuName = "AI/Movement Profile")]
    public class MovementProfile : ScriptableObject
    {
        public EntityType entityType = EntityType.Aggressive;

        private bool IsAggressive => entityType == EntityType.Aggressive || entityType == EntityType.Boss;
        private bool IsTamed => entityType == EntityType.Tamed;

        [Title("Wander")]
        [Tooltip("Normal gezinme hızı (m/s). Withdrawal veya Circling Speed 0 ise bu hız onlara da uygulanır.")]
        public float walkMovementSpeed = 6f;

        [MinMaxSlider(0f, 50f, true)]
        [Tooltip("Wander davranışında rastgele hedef seçilirken kullanılan minimum/maksimum mesafe (metre). Combat Holding Distance veya circle adımlarını değiştirmez.")]
        public Vector2 wanderDistanceRange = new Vector2(12f, 30f);

        public float MinWanderDistance => wanderDistanceRange.x;
        public float MaxWanderDistance => wanderDistanceRange.y;

        [Title("Run")]
        [Tooltip("Approach ve Rush sırasında istenen hız (m/s). Saldırıya yaklaşmayı hızlandırır; circle ve geri çekilme hızlarını değiştirmez.")]
        public float runMovementSpeed = 8f;
        [Tooltip("FleeFromTarget davranışında kaçış hedefleri için kullanılan mesafe aralığı (metre). Yakın dövüş Rush hızı veya adımlar arası bekleme ile ilgili değildir.")]
        public float runDistanceInterval = 2f;

        [TitleGroup("Combat Movement")]
        [LabelText("Withdrawal Speed (m/s)"), Min(0f), Tooltip("Saldırıdan sonra geri çekilme hızı (m/s). 0 ise Walk Movement Speed kullanılır. Mesafe: Maintain Distance modunda Holding Distance, Hold Ground modunda Withdrawal Step Distance.")]
        public float combatWithdrawalSpeed;

        [TitleGroup("Combat Movement")]
        [LabelText("Withdrawal Step Distance (m)"), Min(0f)]
        [Tooltip("Melee Combat Profile > Hold Ground modunda saldırı sonrası bir kez geri gidilecek maksimum mesafe (metre). Oyuncu yaklaşınca hedef uzamaz; 0 ise geri adım atmaz. Arkası kapalıysa daha kısa adım atar veya yerinde kalır. Maintain Distance modunda kullanılmaz.")]
        public float combatWithdrawalStepDistance = 2f;

        [TitleGroup("Combat Movement")]
        [LabelText("Circling Speed (m/s)"), Min(0f), Tooltip("Circle sırasında yana hareket hızı (m/s). 0 ise Walk Movement Speed kullanılır. Bekleme süresini veya circle aşamasının toplam süresini değiştirmez.")]
        public float combatCirclingSpeed;

        [FoldoutGroup("Combat Movement/Circling Steps")]
        [LabelText("Step And Pause"), Tooltip("Circle sırasında sürekli yürümek yerine kısa bir hedefe yürür, varınca bekler. Beklerken hedefe bakma ayarı geçerlidir. Yeniden saldırma zamanı veya mesafe/yön düzeltmesi beklemeyi kesebilir.")]
        public bool combatStepCircling;

        [FoldoutGroup("Combat Movement/Circling Steps"), ShowIf(nameof(combatStepCircling))]
        [LabelText("Step Distance (m)"), Min(0.1f), Tooltip("Maintain Distance modunda oyuncunun çevresindeki yana adımın yaklaşık üst sınırı; mesafe/yön düzeltmesi bunu aşabilir. Hold Ground modunda geri adım sonunda oluşan sabit konumun iki yanında bu uzunlukta kısa bir şerit içinde hareket eder; oyuncu yaklaşınca şerit kaymaz. Çok küçük değerler Waypoint Tolerance nedeniyle hareket üretmeyebilir.")]
        public float combatCircleStepDistance = 1.5f;

        [FoldoutGroup("Combat Movement/Circling Steps"), ShowIf(nameof(combatStepCircling))]
        [LabelText("Pause Between Steps (s)"), MinMaxSlider(0f, 5f, true)]
        [Tooltip("Her kısa adım tamamlandıktan sonra bu aralıktan rastgele seçilen süre kadar bekler. Örn. 0.8–1.5 saniye. İki değer de 0 ise beklemez. Saldırı cooldown'unu değiştirmez; bekleme Circle Duration süresine dahildir.")]
        public Vector2 combatCirclePauseRange = new Vector2(0.8f, 1.5f);

        [TitleGroup("Combat Movement")]
        [LabelText("Attack Approach Spacing"), Range(0.5f, 0.95f)]
        [Tooltip("Rush sonunda kullanılabilir yakın saldırı erişiminin hangi oranında durulacağı. 0.9 = %90; yükseltmek daha uzakta durdurur. Hasar menzilini artırmaz, geri çekilme mesafesini değiştirmez.")]
        public float combatApproachRangeFraction = 0.75f;

        [TitleGroup("Combat Movement")]
        [LabelText("Face Target While Repositioning"), Tooltip("Geri çekilirken, circle hareketinde ve adımlar arasında beklerken hedefe bakar. Kapalıysa hareket yönüne dönme kullanılır. Uygun yana/geri yürüme animasyonları gerekir.")]
        public bool combatFaceTargetWhileRepositioning = true;

        [FoldoutGroup("Combat Movement/Advanced")]
        [LabelText("Orbit Goal Speed (degrees/s)"), Range(5f, 60f), Tooltip("Maintain Distance modunda circle hedefinin oyuncu çevresindeki açısal ilerleme hızı. Gerçek hareket Circling Speed ve Step Distance ile sınırlıdır. Hold Ground kısa yana adımlarında kullanılmaz.")]
        public float combatCircleDegreesPerSecond = 25f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Target Facing Follow Speed (degrees/s)"), Min(1f)]
        [Tooltip("Tercih edilen ön/yan konum bölgesinin hedefin dönüşünü takip etme hızı. Bu robotun fiziksel dönüş hızı değildir; fiziksel dönüş Rotation Module tarafından yönetilir.")]
        public float combatFacingFollowSpeed = 120f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Replan Interval (s)"), Min(0.1f)]
        [Tooltip("Konum kararları arasındaki saniye. Küçük değer hedef değişikliklerine daha hızlı tepki verir fakat daha sık yol hesabı yapar. Adımlı circle mevcut adım hedefini korur; beklerken yeni yol hesaplamaz.")]
        public float combatReplanInterval = 0.35f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Destination Change Threshold (m)"), Min(0.1f)]
        [Tooltip("Yeni hedef önceki hedeften bu kadar az değiştiyse mevcut yol korunur. Yükseltmek küçük değişiklikler için yol yenilemeyi azaltır. Tamamlanmış circle adımları bu eşik yüzünden tutulmaz.")]
        public float combatDestinationChangeThreshold = 0.5f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("Waypoint Tolerance (m)"), Min(0.1f)]
        [Tooltip("Circle/geri çekilme hedefinin kaç metre yakını varış sayılır. Yükseltmek adımı daha erken bitirir. Rush, saldırı menzilinin dışında durmamak için en fazla 0.05 m kullanır.")]
        public float combatWaypointTolerance = 0.3f;

        [FoldoutGroup("Combat Movement/Advanced"), LabelText("NavMesh Sample Radius (m)"), Min(0.1f)]
        [Tooltip("Planlanan konum çevresinde yürünebilir NavMesh noktası arama yarıçapı. Çok küçük değer dar/eğimli zeminde hedef bulmayı zorlaştırır; çok büyük değer hedefi istenen yerden uzaklaştırabilir. Yalnızca tam ulaşılabilir yollar kabul edilir.")]
        public float combatNavMeshSampleRadius = 1f;

        public float GetCombatWithdrawalSpeed() => Mathf.Max(0.1f, combatWithdrawalSpeed > 0f ? combatWithdrawalSpeed : walkMovementSpeed);
        public float GetCombatCirclingSpeed() => Mathf.Max(0.1f, combatCirclingSpeed > 0f ? combatCirclingSpeed : walkMovementSpeed);

        [TitleGroup("Ranged Movement"), Min(0f)]
        [Tooltip("Menzilli saldırılar arasında yana konum değiştirme hızı (m/s). 0 ise Walk Movement Speed. Uzak hedefe yaklaşma hızı Run Movement Speed'dir.")]
        public float rangedRepositionSpeed = 6f;

        [TitleGroup("Ranged Movement"), Min(0f)]
        [Tooltip("Oyuncu çok yaklaşınca kısa kaçış adımının hızı (m/s). 0 ise Run Movement Speed. Kaçışı başlatan mesafe Ranged Combat Profile içindedir.")]
        public float rangedRetreatSpeed = 9f;

        [TitleGroup("Ranged Movement"), Min(0.1f)]
        [Tooltip("Atışlar arasındaki yaklaşık yana adım uzunluğu (metre). Menzil düzeltmesi veya engelden açık atış bulmak için daha farklı bir konum seçilebilir. Hedef seçildikten sonra her karede kaydırılmaz.")]
        public float rangedRepositionStepDistance = 3f;

        [TitleGroup("Ranged Movement"), Min(0.1f)]
        [Tooltip("Oyuncu yaklaşınca bir defada gidilecek maksimum geri/yan kaçış adımı (metre). Oyuncu kovalarken hedef uzamaz. Kapalı yönde daha kısa veya yana bir adım denenir.")]
        public float rangedRetreatStepDistance = 4f;

        [TitleGroup("Combat Tactics", VisibleIf = nameof(IsAggressive))]
        [Tooltip("Ayrı StrafeTarget graph aksiyonunun yana konumlanma mesafesi (metre). Melee circle adımını ayarlamak için Circling Steps > Step Distance kullanılır.")]
        public float strafeDistance = 4f;

        [TitleGroup("Combat Tactics", VisibleIf = nameof(IsAggressive))]
        [Tooltip("Ayrı FlankTarget graph aksiyonunun hedef çevresindeki mesafesi (metre). Melee Combat Profile'ın Flank açısı veya Holding Distance ayarı değildir.")]
        public float flankDistance = 6f;

        [TitleGroup("Search", VisibleIf = nameof(IsAggressive))]
        public float searchMovementSpeed = 6f;

        [TitleGroup("Search", VisibleIf = nameof(IsAggressive))]
        public float lookAroundDuration = 2f;

        [TitleGroup("Search", VisibleIf = nameof(IsAggressive))]
        [MinMaxSlider(0f, 20f, true)]
        public Vector2 searchDistanceRange = new Vector2(4f, 7f);

        [TitleGroup("Follow", VisibleIf = nameof(IsTamed))]
        public float teleportRadius = 70f;

        [TitleGroup("Follow", VisibleIf = nameof(IsTamed))]
        public float stopRadius = 8f;

        [TitleGroup("Follow", VisibleIf = nameof(IsTamed))]
        public float followRadius = 18f;

        public float MinSearchDistance => searchDistanceRange.x;
        public float MaxSearchDistance => searchDistanceRange.y;
    }
}
