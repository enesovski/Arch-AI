using Artifika.AI.Aggro.Reactions;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

namespace Artifika.AI.Visuals
{
    public class AggroEyeColorReaction : MonoBehaviour, IAggroReaction
    {
        private static readonly int EmissionColor = Shader.PropertyToID(EmissionProperty);

        [Title("References")] 
        [SerializeField] private Light eyeLight;             
        [SerializeField] private LensFlareComponentSRP srpLensFlare;
        [SerializeField] private Renderer eyeRenderer;
        [SerializeField] private int materialIndex;
    
        [Title("Configuration")]
        [SerializeField] [LabelText("Robot Eye Configuration")] private RobotEyeConfiguration config;
        
        private Blackboard _blackboard;
        private Material _eyeMat;
        private const string EmissionProperty = "_EmissionColor";

        public void Initialize(Blackboard blackboard)
        {
            _eyeMat = eyeRenderer.materials[materialIndex];
            _eyeMat.EnableKeyword("_EMISSION");
            eyeLight.color = config.passiveColor;

            OnAggroStateChanged(new AggroStateChangeEventArgs(
                previousState: AggroState.Passive,
                newState: AggroState.Passive,
                target: null
            ));
 
        }

        public void OnAggroStateChanged(AggroStateChangeEventArgs args)
        {
            Color col;
            float eyeInt, flareInt, flareScale;

            var state = args.NewState;

            switch (state)
            {
                case AggroState.Passive:
                    col = config.passiveColor;
                    eyeInt = config.passiveEyeLigthIntensity;
                    flareInt = config.passiveFlareIntensity;
                    flareScale = config.passiveFlareScale;
                    break;

                case AggroState.Suspicious:
                    col = config.threatenedColor;
                    eyeInt = config.threatenedEyeLightIntensity;
                    flareInt = config.threatenedFlareIntensity;
                    flareScale = config.threatenedFlareScale;
                    break;

                case AggroState.Alerted:
                    col = config.aggressiveColor;
                    eyeInt = config.aggressiveEyeLightIntensity;
                    flareInt = config.aggressiveFlareIntensity;
                    flareScale = config.aggressiveFlareScale;
                    break;

                default:
                    return;
            }

            DOTween.Kill(_eyeMat);
            DOTween.To(
                () => _eyeMat.GetColor(EmissionColor).maxColorComponent,
                v =>
                {
                    _eyeMat.SetColor(EmissionColor, col * v);
                },
                eyeInt,
                config.fadeDuration
            ).SetTarget(_eyeMat).SetEase(config.easeMethod);

            DOTween.Kill(eyeLight);
            eyeLight.DOColor(col, config.fadeDuration).SetTarget(eyeLight).SetEase(config.easeMethod);
            eyeLight.DOIntensity(eyeInt, config.fadeDuration).SetTarget(eyeLight).SetEase(config.easeMethod);

            DOTween.Kill(srpLensFlare);
            DOTween.To(() => srpLensFlare.intensity, x => srpLensFlare.intensity = x, flareInt, config.fadeDuration)
                .SetTarget(srpLensFlare).SetEase(config.easeMethod);
            DOTween.To(() => srpLensFlare.scale, x => srpLensFlare.scale = x, flareScale, config.fadeDuration)
                .SetTarget(srpLensFlare).SetEase(config.easeMethod);
        }
    }
}