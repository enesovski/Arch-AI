using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DissolveHandler : MonoBehaviour
{
    [Title("Start")]
    public bool callInAwake = true;

    public float initialDelay = 0.25f;

    [Title("Dissolve")]
    public float dissolveSeconds = 1.5f;
    public string dissolvePropertyName = "_Dissolve";
    public bool UseAnimationCurve = true;

    public AnimationCurve DissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Range(0f, 1f)] public float StartValue = 0f;
    [Range(0f, 1f)] public float EndValue = 1f;

    [Title("Aftermath")]
    public float DestroyDelay = 1f;

    [Title("Audio")]
    public AudioSource dissolveSoundLoop;

    struct RendererSlot
    {
        public Renderer renderer;
        public int submeshIndex;
        public bool supportsProperty;
    }

    readonly List<RendererSlot> slots = new List<RendererSlot>(16);
    readonly Dictionary<Material, bool> propertySupportCache = new Dictionary<Material, bool>(16);

    MaterialPropertyBlock propertyBlock;
    int dissolvePropertyId;
    Coroutine running;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        dissolvePropertyId = Shader.PropertyToID(dissolvePropertyName);

        var renderers = GetComponentsInChildren<Renderer>(true);

        slots.Clear();
        propertySupportCache.Clear();

        foreach (var r in renderers)
        {
            if (r == null) continue;

            var shared = r.sharedMaterials; 
            int count = shared != null ? shared.Length : 0;

            for (int i = 0; i < count; i++)
            {
                var mat = shared[i];
                bool supports = false;

                if (mat != null)
                {
                    if (!propertySupportCache.TryGetValue(mat, out supports))
                    {
                        supports = mat.HasProperty(dissolvePropertyId);
                        propertySupportCache.Add(mat, supports);
                    }
                }

                slots.Add(new RendererSlot
                {
                    renderer = r,
                    submeshIndex = i,
                    supportsProperty = supports
                });
            }
        }

        ApplyDissolveValue(StartValue);

        if (callInAwake)
            StartDissolve();
    }

    void OnDisable()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    public void StartDissolve()
    {
        if (running == null)
            running = StartCoroutine(DissolveRoutine());
    }

    IEnumerator DissolveRoutine()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        dissolveSoundLoop.Play();

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, dissolveSeconds);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = UseAnimationCurve ? DissolveCurve.Evaluate(t) : t;
            float value = Mathf.Lerp(StartValue, EndValue, eased);

            ApplyDissolveValue(value);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyDissolveValue(EndValue);
        dissolveSoundLoop?.Stop();

        if (DestroyDelay > 0f)
            yield return new WaitForSeconds(DestroyDelay);

        Destroy(gameObject);
        running = null;
    }

    void ApplyDissolveValue(float value)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.renderer == null) continue;
            if (!s.supportsProperty) continue; 

            s.renderer.GetPropertyBlock(propertyBlock, s.submeshIndex);
            propertyBlock.SetFloat(dissolvePropertyId, value);
            s.renderer.SetPropertyBlock(propertyBlock, s.submeshIndex);
        }
    }
}
