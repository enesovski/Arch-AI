using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Artifika.AI.Sensors
{
    [Title("Audio Sensor")]
    public class AudioSensor : BaseSensor
    {
        [SerializeField, Min(0f)]
        private float hearingRadius = 10f;

        [SerializeField]
        private LayerMask targetMask;

        private readonly Collider[] _overlaps = new Collider[DetectionModule.MAX_TARGET_COUNT];

        public override void Detect()
        {
            int count = Physics.OverlapSphereNonAlloc(owner.position, hearingRadius, _overlaps, targetMask);
            float radiusInv = 1f / hearingRadius;

            for (int i = 0; i < count; i++)
            {
                Collider col = _overlaps[i];
                if (!col.TryGetComponent(out GameEntity entity))
                    continue;

                var sources = entity.GetComponentsInChildren<AudioSource>();
                foreach (var src in sources)
                {
                    if (!src.isPlaying)
                        continue;

                    float dist = Vector3.Distance(owner.position, src.transform.position);
                    if (dist > hearingRadius)
                        continue;

                    float rawStrength = src.volume * (1f - dist * radiusInv);
                    if (rawStrength > 0f)
                        Emit(entity, rawStrength);

                    break;
                }
            }

        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (owner == null) owner = transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.position, hearingRadius);

            Handles.Label(owner.position + -Vector3.forward * (hearingRadius + 0.2f), "Audio Sensor");

        }
#endif
    }
}
