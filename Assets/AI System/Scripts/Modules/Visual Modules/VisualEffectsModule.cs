using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public class VisualEffectsModule : NetworkBehaviour, IAnimationEffectPlayer
    {
        [Serializable]
        public struct EffectEntry
        {
            public string key;
            public MMF_Player player;
        }

        [Title("References")]
        [Tooltip("Component implementing " + nameof(IEntityHealth) + ". Resolved from children when empty.")]
        [FormerlySerializedAs("healthComponent")]
        [SerializeField] private MonoBehaviour healthSource;

        [Title("Effects")]
        [Tooltip("Optional component implementing " + nameof(IHitHighlight) + ".")]
        [FormerlySerializedAs("highlightEffect")]
        [SerializeField] private MonoBehaviour hitHighlight;
        [Space]
        [SerializeField] private MMF_Player deathFeedback;
        [SerializeField] private MMF_Player hitFeedback;

        [Title("Effect Entries")]
        [SerializeField] private List<EffectEntry> effectEntries = new();

        private readonly Dictionary<string, MMF_Player> map = new Dictionary<string, MMF_Player>(StringComparer.Ordinal);
        private IEntityHealth healthComponent;

        private void Start()
        {
            healthComponent = healthSource as IEntityHealth ?? GetComponentInChildren<IEntityHealth>();

            if (healthComponent != null)
            {
                healthComponent.OnDamageReceived += PlayDamagedFX;
                healthComponent.OnDeath += PlayDeathFX;
            }

            RebuildMap();
        }

        private void RebuildMap()
        {
            map.Clear();

            for (int i = 0; i < effectEntries.Count; i++)
            {
                EffectEntry effectEntry = effectEntries[i];
                if (!map.ContainsKey(effectEntry.key))
                {
                    map.Add(effectEntry.key, effectEntry.player);
                }
            }
        }

        public void PlayEffect(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!map.TryGetValue(key, out MMF_Player player) || player == null)
            {
                return;
            }
            player?.PlayFeedbacks();
        }

        private void PlayDeathFX()
        {
            StartDeathFeedback();
        }

        private void StartDeathFeedback()
        {
            deathFeedback?.PlayFeedbacks();
            StartCoroutine(StopFeedbackAfterDuration(deathFeedback, 2));
        }

        private void PlayDamagedFX(DamageReport report)
        {
            StartHitFeedback(report.ImpactPoint);
        }

        private void StartHitFeedback(Vector3 impactPoint)
        {
            (hitHighlight as IHitHighlight)?.PlayHitHighlight(impactPoint);
            if (hitFeedback != null)
                hitFeedback.transform.position = impactPoint;
            hitFeedback?.PlayFeedbacks();
        }

        private IEnumerator StopFeedbackAfterDuration(MMF_Player feedback, float duration)
        {
            yield return new WaitForSeconds(duration);

            feedback?.StopFeedbacks();
        }

        public void PlayVisualEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            GameObject effectInstance = Instantiate(effectPrefab, position, rotation);
            Destroy(effectInstance, 10);
        }

        public void PlayRandomClip(AudioClip[] clips)
        {
            int index = UnityEngine.Random.Range(0, clips.Length);
        }

        public override void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDamageReceived -= PlayDamagedFX;
                healthComponent.OnDeath -= PlayDeathFX;
                healthComponent = null;
            }

            base.OnDestroy();
        }
    }
}
