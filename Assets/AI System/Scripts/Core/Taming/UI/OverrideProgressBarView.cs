using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ReachProgressBar = Michsky.UI.Reach.ProgressBar;

namespace Artifika.UI.Override
{
    [DisallowMultipleComponent]
    public sealed class OverrideProgressBarView : MonoBehaviour
    {
        [SerializeField] private ReachProgressBar reachProgressBar;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image glowImage;

        private OverrideSkillCheckUIConfigSO _config;
        private Tween _fillTween;
        private Tween _pulseTween;
        private bool _filled;

        public RectTransform RectTransform => (RectTransform)transform;

        private void Awake()
        {
            EnsureReachProgressBar();
        }

        public void Configure(OverrideSkillCheckUIConfigSO config)
        {
            _config = config;
            EnsureReachProgressBar();

            if (backgroundImage != null)
                backgroundImage.color = WithAlpha(config.PrimaryColor, 0.16f);

            if (fillImage != null)
                fillImage.color = config.AccentColor;

            if (glowImage != null)
                glowImage.color = WithAlpha(config.AccentColor, 0f);

            ResetImmediate();
        }

        public void SetFilled(bool filled, bool animated)
        {
            if (_filled == filled && animated)
                return;

            _filled = filled;
            KillTweens();

            float targetValue = filled ? 1f : 0f;
            if (!animated || _config == null || _config.FeedbackAnimationDuration <= 0f)
            {
                reachProgressBar.SetValue(targetValue);
                SetGlowAlpha(filled ? 0.45f : 0f);
                return;
            }

            _fillTween = DOTween.To(
                    () => reachProgressBar.currentValue,
                    reachProgressBar.SetValue,
                    targetValue,
                    _config.FeedbackAnimationDuration)
                .SetEase(filled ? Ease.OutCubic : Ease.InCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (!filled)
            {
                if (glowImage != null)
                    glowImage.DOFade(0f, _config.FeedbackAnimationDuration).SetUpdate(true);
                return;
            }

            if (glowImage != null)
            {
                glowImage.DOFade(0.65f, _config.FeedbackAnimationDuration * 0.45f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            _pulseTween = transform.DOPunchScale(
                    Vector3.one * 0.12f,
                    _config.FeedbackAnimationDuration,
                    5,
                    0.55f)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void ResetImmediate()
        {
            _filled = false;
            KillTweens();
            EnsureReachProgressBar();
            reachProgressBar.SetValue(0f);
            transform.localScale = Vector3.one;
            SetGlowAlpha(0f);
        }

        private void EnsureReachProgressBar()
        {
            if (reachProgressBar == null)
                reachProgressBar = GetComponent<ReachProgressBar>();

            if (reachProgressBar == null)
                return;

            reachProgressBar.minValue = 0f;
            reachProgressBar.maxValue = 1f;
            reachProgressBar.barDirection = ReachProgressBar.BarDirection.Left;
            reachProgressBar.barImage = fillImage;
            reachProgressBar.onValueChanged ??= new ReachProgressBar.OnValueChanged();
        }

        private void KillTweens()
        {
            _fillTween?.Kill();
            _pulseTween?.Kill();
            _fillTween = null;
            _pulseTween = null;
        }

        private void SetGlowAlpha(float alpha)
        {
            if (glowImage == null)
                return;

            Color color = glowImage.color;
            color.a = alpha;
            glowImage.color = color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void OnDisable()
        {
            KillTweens();
            transform.localScale = Vector3.one;
        }
    }
}
