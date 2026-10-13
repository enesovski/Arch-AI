using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artifika.AI.Sensors
{
    [Title("Visual Sensor")]
    public class VisualSensor : BaseSensor
    {
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private LayerMask obstacleMask;

        public LayerMask ObstacleMask => obstacleMask;

        [Title("Main Sensor")] 
        [SerializeField] private float viewRadius = 15f;
        [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;
        
        [Title("Secondary Sensor")] 
        [SerializeField]
        private bool enableSecondarySensor = true;

        [SerializeField, ShowIf(nameof(enableSecondarySensor))] 
        [MinValue(0f), LabelText("View Radius")]
        private float proximityRadius = 3f;

        [SerializeField, ShowIf(nameof(enableSecondarySensor))] 
        [MinMaxSlider(0, 1f), LabelText("Sensor Strength Interval")]
        private Vector2 proximityStrengthInterval = new Vector2(0.4f, 1);

        [SerializeField, ShowIf(nameof(enableSecondarySensor)),
        LabelText("Ignore Obstacles")]
        private bool proximityIgnoresObstacles = true;


        private Transform origin;
        private float cosHalf;
        private float proxRadiusSq;

        private const int MaxOverlapCapacity = 1024;
        private Collider[] overlaps = new Collider[DetectionModule.MAX_TARGET_COUNT];
        private readonly HashSet<GameEntity> seenEntities = new HashSet<GameEntity>();
        private bool loggedOverlapOverflow;

        public override void Initialize(GameEntity owner)
        {
            base.Initialize(owner);
            origin = transform;
            cosHalf = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
            CacheProximityValues();
        }

        public override void Detect()
        {
            Vector3 originPos = origin.position;

            int count;
            do
            {
                count = Physics.OverlapSphereNonAlloc(originPos, viewRadius, overlaps, targetMask);
                if (count < overlaps.Length || overlaps.Length >= MaxOverlapCapacity)
                    break;

                System.Array.Resize(ref overlaps, Mathf.Min(overlaps.Length * 2, MaxOverlapCapacity));
            } while (true);

            if (count == overlaps.Length && !loggedOverlapOverflow)
            {
                Debug.LogWarning($"[VisualSensor] Overlap buffer is full ({overlaps.Length}) on {name}; some targets may be missed.", this);
                loggedOverlapOverflow = true;
            }

            seenEntities.Clear();
            Vector3 flatForward = origin.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.0001f)
                flatForward.Normalize();

            for (int i = 0; i < count; i++)
            {
                Collider collider = overlaps[i];
                if (!collider) continue;

                GameEntity entity = collider.GetComponentInParent<GameEntity>();
                if (!entity || entity == owner || !seenEntities.Add(entity)) continue;

                Vector3 toTarget = entity.AimPosition - originPos;
                float distSq = toTarget.sqrMagnitude;
                float dist = Mathf.Sqrt(distSq);
                if (dist < 0.0001f) continue;
                Vector3 dir = toTarget / dist;

                Vector3 flatDirection = toTarget;
                flatDirection.y = 0f;
                bool withinCone = flatDirection.sqrMagnitude < 0.0001f ||
                                  (flatForward.sqrMagnitude > 0f &&
                                   Vector3.Dot(flatForward, flatDirection.normalized) >= cosHalf);

                if (withinCone && HasClearSight(originPos, dir, dist, entity))
                {
                    float coneStrength = 1f - (dist / viewRadius);
                    Emit(entity, coneStrength);
                }

                if (enableSecondarySensor && distSq <= proxRadiusSq)
                {
                    bool canEmit = proximityIgnoresObstacles
                                   || HasClearSight(originPos, dir, dist, entity);

                    if (canEmit)
                    {
                        float t01 = dist / proximityRadius;
                        float proxStrength = Mathf.Lerp(proximityStrengthInterval.x, proximityStrengthInterval.y, t01);
                        Emit(entity, proxStrength);
                    }
                }
            }
        }

        private bool HasClearSight(Vector3 originPos, Vector3 direction, float distance, GameEntity target)
        {
            if (!Physics.Raycast(originPos, direction, out RaycastHit hit, distance, obstacleMask))
                return true;

            // A target may have child colliders on an obstacle layer. Seeing its
            // own collider must not count as being blocked by the environment.
            return hit.collider.GetComponentInParent<GameEntity>() == target;
        }

        private void CacheProximityValues()
        {
            proxRadiusSq = proximityRadius * proximityRadius;
        }

        private void OnValidate()
        {
            if (proximityRadius > viewRadius)
            {
                Debug.LogWarning(
                    $"[VisualSensor] proximityRadius ({proximityRadius}) exceeds viewRadius ({viewRadius}). "
                    + "Clamping to viewRadius. Exceeding it would require a separate physics query.",
                    this);
                proximityRadius = viewRadius;
            }

            CacheProximityValues();
            cosHalf = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        }

        private Vector3 DirFromAngle(float angleDeg)
        {
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.forward;

            return Quaternion.Euler(0f, angleDeg, 0f) * flatForward.normalized;
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;

            Color mainColor = new Color(1f, 1f, 0f, 0.15f);
            Color mainWireColor = new Color(1f, 1f, 0f, 1f);

            Color proximityColor = new Color(1f, 0.4f, 0f, 0.08f);
            Color proximityWireColor = new Color(1f, 0.4f, 0f, 1f);

            Vector3 left = DirFromAngle(-viewAngle * 0.5f);
            Vector3 right = DirFromAngle(viewAngle * 0.5f);

            Gizmos.color = mainWireColor;
            Gizmos.DrawLine(pos, pos + left * viewRadius);
            Gizmos.DrawLine(pos, pos + right * viewRadius);
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(pos, viewRadius);

            Handles.color = mainColor;
            Handles.DrawSolidArc(pos, Vector3.up, left, viewAngle, viewRadius);

            Handles.color = mainWireColor;
            Handles.DrawWireArc(pos, Vector3.up, left, viewAngle, viewRadius);

            if (enableSecondarySensor)
            {
                Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
                Gizmos.DrawWireSphere(pos, proximityRadius);
                Handles.color = proximityColor;
                Handles.DrawSolidDisc(pos, Vector3.up, proximityRadius);

                Handles.color = proximityWireColor;
                Handles.DrawWireDisc(pos, Vector3.up, proximityRadius);
            }
        }
        #endif
    }
}
