using DG.Tweening;
using UnityEngine;

namespace Artifika.AI.Visuals
{
    [CreateAssetMenu(fileName = "Robot Eye Configuration", menuName = "AI/Visuals/Robot Eye Configuration", order = 0)]
    public class RobotEyeConfiguration : ScriptableObject
    {
        [Header("Animation Settings")]
        public float fadeDuration = 0.5f;
        public Ease easeMethod;

        [Header("Visuals")]
        [Space]
        public float passiveEyeLigthIntensity = 2f;
        public float passiveFlareIntensity = 0.2f;
        public float passiveFlareScale = 0.2f;
        [ColorUsage(true, true)]
        public Color passiveColor = new Color32(0x00, 0xBF, 0xFF, 0xFF);

        [Space]
        public float threatenedEyeLightIntensity = 2f;
        public float threatenedFlareIntensity = 0.2f;
        public float threatenedFlareScale = 0.2f;
        [ColorUsage(true, true)]
        public Color threatenedColor = new Color32(0x00, 0xBF, 0xFF, 0xFF);

        [Space]
        public float aggressiveEyeLightIntensity = 2f;
        public float aggressiveFlareIntensity = 0.2f;
        public float aggressiveFlareScale = 0.2f;
        [ColorUsage(true, true)]
        public Color aggressiveColor = new Color32(0x00, 0xBF, 0xFF, 0xFF);

        [Header("Attack Telegraph")]
        public float telegraphEyeLightIntensity = 3f;
        public float telegraphFlareIntensity = 0.4f;
        public float telegraphFlareScale = 0.4f;
        [Min(0.01f)]
        public float telegraphPulseHalfDuration = 0.15f;
        [Min(0f)]
        public float telegraphReturnDuration = 0.1f;

        [Space]
        public float stunnedEyeLightIntensity = 2f;
        public float stunnedFlareIntensity = 0.2f;
        public float stunnedFlareScale = 0.2f;
        [ColorUsage(true, true)]
        public Color stunnedColor = new Color32(0xA0, 0x30, 0xFF, 0xFF);
    }
}
