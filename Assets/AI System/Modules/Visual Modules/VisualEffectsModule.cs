using HighlightPlus;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class VisualEffectsModule : NetworkBehaviour
{

    [Serializable]
    public struct EffectEntry
    {
        public string key;
        public MMF_Player player;
    }

    [Title("References")]
    [SerializeField] private HealthComponent healthComponent;

    [Title("Effects")]
    [SerializeField] HighlightEffect highlightEffect;
    [Space]
    [SerializeField] MMF_Player deathFeedback;
    [SerializeField] MMF_Player hitFeedback;


    [Title("Effect Entries")]
    [SerializeField] private List<EffectEntry> effectEntries = new();

    private readonly Dictionary<string, MMF_Player> map = new Dictionary<string, MMF_Player>(StringComparer.Ordinal);


    private void Start()
    {

        healthComponent.OnDamageTakenArgs += PlayDamagedFX;
        healthComponent.OnDeath += PlayDeathFX;

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


    void PlayDeathFX()
    {
        StartDeathFeedback();
        PlayDeathFeedbackClientRpc();
    }

    void StartDeathFeedback()
    {
        deathFeedback?.PlayFeedbacks();
        StartCoroutine(StopFeedbackAfterDuration(deathFeedback, 2));
    }

    [ClientRpc]
    void PlayDeathFeedbackClientRpc()
    {
        if (IsHost)
        {
            return;
        }

        StartDeathFeedback();
    }

    void PlayDamagedFX(DamageInstance damageData, float damageAmount)
    {
        StartHitFeedback(damageData.Context.ImpactPoint);
        PlayHitFeedbackClientRpc(damageData.Context.ImpactPoint);
        
    }

    void StartHitFeedback(Vector3 impactPoint)
    {
        highlightEffect?.HitFX();
        hitFeedback.transform.position = impactPoint;
        hitFeedback?.PlayFeedbacks();
    }

    [ClientRpc]
    void PlayHitFeedbackClientRpc(Vector3 impactPoint)
    {
        if (IsHost)
        {
            return;
        }

        StartHitFeedback(impactPoint);
    }

    IEnumerator StopFeedbackAfterDuration(MMF_Player feedback, float duration)
    {
        yield return new WaitForSeconds(duration);

        feedback.StopFeedbacks();
    }

    public void PlayVisualEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
    {
        GameObject effectInstance = Instantiate(effectPrefab, position, rotation);
        Destroy(effectInstance, 10);
    }

    public void PlayRandomClip(AudioClip[] clips)
    {
        int index = UnityEngine.Random.Range(0, clips.Length);
        AudioPoolManager.Instance.PlaySound(clips[index], transform.position, volume: 0.5f);
    }

    public override void OnDestroy()
    {
        ILivingEntity livingEntity = GetComponentInChildren<ILivingEntity>();

        if (livingEntity != null)
        {
            livingEntity.OnDeath -= PlayDeathFX;
        }

        base.OnDestroy();
    }
}
