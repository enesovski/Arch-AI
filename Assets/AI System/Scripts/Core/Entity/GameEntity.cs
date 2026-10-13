using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public class GameEntity : MonoBehaviour
    {
        [Title("Identity")] [SerializeField] private EntityType entityType;
        public EntityType EntityType => entityType;
        [SerializeField] private FactionType faction;
        public FactionType Faction => faction;

        [Space] [ShowIf("entityType", EntityType.Tamed)]
        public NetworkObject owner;

        [HideIf("entityType", EntityType.Player)]
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Boxed), AssetsOnly]
        public EntityConfiguration entityConfiguration;

        [Title("Targeting")]
        [SerializeField] private bool useAimOffset;
        [SerializeField, ShowIf(nameof(useAimOffset))] private Vector3 aimOffset;

        public bool UseAimOffset => useAimOffset;
        public Vector3 AimOffset => useAimOffset ? aimOffset : Vector3.zero;
        public Vector3 AimPosition => useAimOffset ? transform.TransformPoint(aimOffset) : transform.position;

        [Tooltip("Optional server-visible facing transform. Defaults to this entity's root.")]
        [SerializeField] private Transform combatFacing;
        [Tooltip("Optional body/hurtbox colliders for combat range. Empty automatically caches this entity's colliders. Call RebuildCombatColliders after adding hurtboxes at runtime.")]
        [SerializeField] private Collider[] combatColliders;
        private Collider[] cachedCombatColliders;

        public Vector3 CombatForward => (combatFacing ? combatFacing : transform).forward;

        public HealthComponent HealthComponent { get; private set; }
        public NetworkObject NetworkObject { get; private set; }
        public AggroTracker AggroTracker { get; private set; }

        protected virtual void Awake()
        {
            HealthComponent = GetComponent<HealthComponent>();
            NetworkObject = GetComponent<NetworkObject>();
            AggroTracker = GetComponent<AggroTracker>() ?? GetComponentInChildren<AggroTracker>(true);

            if (entityConfiguration != null && entityType != EntityType.Player && entityType != EntityType.Tamed)
            {
                entityType = entityConfiguration.baseEntityType;
            }
        }

        public void SetEntityType(EntityType newType)
        {
            entityType = newType;
        }

        public string GetAnalyticsDisplayId() =>
            entityConfiguration ? entityConfiguration.entityName : gameObject.name;

        public void SetOwner(NetworkObject newOwner)
        {
            owner = newOwner;
        }

        public bool HasOwner()
        {
            return owner != null;
        }

        public void RebuildCombatColliders()
        {
            Collider[] candidates = combatColliders != null && combatColliders.Length > 0
                ? combatColliders : GetComponentsInChildren<Collider>(true);
            var owned = new System.Collections.Generic.List<Collider>(candidates.Length);
            foreach (Collider candidate in candidates)
            {
                // Nested entities (riders, companions, etc.) have their own geometry.
                if (candidate && candidate.GetComponentInParent<GameEntity>() == this)
                    owned.Add(candidate);
            }
            cachedCombatColliders = owned.ToArray();
        }

        public bool TryGetClosestCombatPoint(Vector3 origin, int layerMask, out Vector3 point)
        {
            if (cachedCombatColliders == null)
                RebuildCombatColliders();

            point = transform.position;
            float closestSq = float.PositiveInfinity;
            foreach (Collider body in cachedCombatColliders)
            {
                if (!IsCombatColliderUsable(body, layerMask)) continue;
                Vector3 candidate = body.ClosestPoint(origin);
                float distanceSq = (candidate - origin).sqrMagnitude;
                if (distanceSq >= closestSq) continue;
                closestSq = distanceSq;
                point = candidate;
            }
            return !float.IsPositiveInfinity(closestSq);
        }

        // Bounds provide a conservative navigation footprint; attack range uses
        // ClosestPoint on the actual shapes above, never this enclosing box.
        public bool TryGetCombatBounds(int layerMask, out Bounds bounds)
        {
            if (cachedCombatColliders == null)
                RebuildCombatColliders();

            bounds = default;
            bool found = false;
            foreach (Collider body in cachedCombatColliders)
            {
                if (!IsCombatColliderUsable(body, layerMask)) continue;
                if (found) bounds.Encapsulate(body.bounds);
                else bounds = body.bounds;
                found = true;
            }
            return found;
        }

        private static bool IsCombatColliderUsable(Collider body, int layerMask)
        {
            return body && body.enabled && body.gameObject.activeInHierarchy &&
                   !body.isTrigger && (layerMask & (1 << body.gameObject.layer)) != 0;
        }
    }
}
