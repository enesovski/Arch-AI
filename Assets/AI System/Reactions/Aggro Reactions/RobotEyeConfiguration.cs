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

    }
}