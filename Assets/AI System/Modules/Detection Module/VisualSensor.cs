using System;
using UnityEngine;
using Sirenix.OdinInspector;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artifika.AI.Sensors
{
    [Title("Visual Sensor")]
    public class VisualSensor : BaseSensor
    {

        [Title("Detection Settings")]
        [SerializeField] private float viewRadius = 15f;

        [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;

        [SerializeField] private LayerMask targetMask;

        [SerializeField] private LayerMask obstacleMask;

        private Transform origin;

        private readonly Collider[] overlaps = new Collider[DetectionModule.MAX_TARGET_COUNT];

        private int detectedCount;
        private Transform detectedTarget;

        private float cosHalf;
        public override void Initialize(Transform owner)
        {
            base.Initialize(owner);
            origin = transform;
            cosHalf = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);

        }

        public override void Detect()
        {
            //using (GCAllocationProbe.Begin("VisualSensor.Detect"))
            //{
            //    //Debug.Log("VisualSensor Detect called");
            //    using (GCAllocationProbe.Begin("VisualSensor.Detect.OverlapSphereNonAlloc"))
            //    {
                    //int count = Physics.OverlapSphereNonAlloc(origin.position, viewRadius, overlaps, targetMask);
                    detectedCount = Physics.OverlapSphereNonAlloc(origin.position, viewRadius, overlaps, targetMask);

                    //using (GCAllocationProbe.Begin("VisualSensor.Detect.ForLoop"))
                    //{
                        for (int i = 0; i < detectedCount; i++)
                        {

                            detectedTarget = overlaps[i].transform;


                            if (!detectedTarget.TryGetComponent<GameEntity>(out var entity))
                                continue;


                            Vector3 toTarget = (detectedTarget.position - origin.position);
                            float dist = toTarget.magnitude;
                            Vector3 dir = toTarget / dist;

                            if (Vector3.Dot(origin.forward, dir) < cosHalf) continue;
                            if (Physics.Raycast(origin.position, dir, dist, obstacleMask)) continue;

                            float rawStrength = 1f - (dist / viewRadius);
                            Emit(entity, rawStrength);
                        }
                //    }
                //}

            //}
        }
    
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 leftBoundary = DirFromAngle(-viewAngle / 2f);
            Vector3 rightBoundary = DirFromAngle(viewAngle / 2f);

            Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

            Handles.color = new Color(1f, 1f, 0f, 0.15f);
            Handles.DrawSolidArc(transform.position, Vector3.up, leftBoundary, viewAngle, viewRadius);

            Handles.color = Color.cyan;
            Handles.Label(transform.position + -Vector3.forward * (viewRadius + 0.2f), "Visual Sensor");

        }
#endif

        private Vector3 DirFromAngle(float angleDeg)
        {
            return Quaternion.Euler(0, angleDeg, 0) * transform.forward;
        }

    }


}

