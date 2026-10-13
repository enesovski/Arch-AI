using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Artifika.UI.Override
{
    [DisallowMultipleComponent]
    public sealed class OverrideTimingRingMinigameView : OverrideMinigameView
    {
        [Header("Motion")]
        [SerializeField] private RectTransform timingRingRoot;
        [SerializeField] private RectTransform ambientHaloRoot;
        [SerializeField] private RectTransform successZoneRoot;
        [SerializeField] private RectTransform needlePivot;
        [SerializeField] private RectTransform centerPulseRoot;
        [SerializeField] private RectTransform impactRippleRoot;
        [SerializeField] private RectTransform directionBadgeRoot;

        [Header("Graphics")]
        [SerializeField] private Image ringTrackImage;
        [SerializeField] private Image ambientHaloImage;
        [SerializeField] private Image successZoneImage;
        [SerializeField] private Image successZoneGlowImage;
        [SerializeField] private Image needleImage;
        [SerializeField] private Image needleGlowImage;
        [SerializeField] private Image centerFrameImage;
        [SerializeField] private Image resultFlashImage;
        [SerializeField] private Image impactRippleImage;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI directionText;

        private LocalOverrideSkillCheckData _activeChallenge;
        private bool _challengeActive;
        private Tween _resultTween;
        private Tween _centerPulseTween;
        private Tween _zonePulseTween;
        private Tween _haloTween;
        private Tween _ringMotionTween;
        private Tween _directionTween;
        private Tween _resultLabelTween;
        private Tween _zoneImpactTween;
        private Tween _needleImpactTween;
        private Sequence _impactSequence;
        private Vector2 _resultLabelRestPosition;

        public override OverrideMinigameType Type => OverrideMinigameType.TimingRing;

        public override void Configure(OverrideSkillCheckUIConfigSO config)
        {
            base.Configure(config);

            if (resultText != null)
                _resultLabelRestPosition = resultText.rectTransform.anchoredPosition;

            if (timingRingRoot != null)
                timingRingRoot.sizeDelta = Vector2.one * config.TimingRingDiameter;

            if (ringTrackImage != null)
                ringTrackImage.color = WithAlpha(config.PrimaryColor, 0.14f);

            if (ambientHaloImage != null)
                ambientHaloImage.color = WithAlpha(config.AccentColor, 0.08f);

            if (successZoneImage != null)
                successZoneImage.color = config.SecondaryColor;

            if (successZoneGlowImage != null)
                successZoneGlowImage.color = WithAlpha(config.SecondaryColor, 0.22f);

            if (needleImage != null)
                needleImage.color = config.PrimaryColor;

            if (needleGlowImage != null)
                needleGlowImage.color = WithAlpha(config.AccentColor, 0.18f);

            if (centerFrameImage != null)
                centerFrameImage.color = WithAlpha(config.BackgroundColor, 0.94f);

            if (resultFlashImage != null)
                resultFlashImage.color = WithAlpha(config.NegativeColor, 0f);

            if (impactRippleImage != null)
                impactRippleImage.color = WithAlpha(config.AccentColor, 0f);
        }

        public void BeginChallenge(LocalOverrideSkillCheckData challenge)
        {
            _activeChallenge = challenge;
            _challengeActive = true;

            ConfigureSuccessZone(challenge.SuccessZoneStart, challenge.SuccessZoneSize);
            SetIndicatorPosition(challenge.NeedleStartPosition);
            SetResult("SYNC WINDOW", Config != null ? Config.SecondaryColor : Color.yellow);
            SetDirection(challenge.Clockwise, challenge.DurationSeconds, false);
            PlayRoundStart(challenge.Clockwise);
            StartAmbientMotion();
        }

        public void Tick(double serverTime)
        {
            if (!_challengeActive)
                return;

            float position = _activeChallenge.GetNormalizedIndicatorPosition(serverTime);
            SetIndicatorPosition(position);
            UpdateNeedleProximity(_activeChallenge.Contains(position));
        }

        public void Resolve(float evaluatedIndicatorPosition, OverrideSkillCheckOutcome outcome, bool animated)
        {
            _challengeActive = false;
            SetIndicatorPosition(evaluatedIndicatorPosition);
            UpdateNeedleProximity(outcome == OverrideSkillCheckOutcome.Success);
            ShowOutcome(outcome, animated);
        }

        public override void ShowOutcome(OverrideSkillCheckOutcome outcome, bool animated)
        {
            bool directionWillToggle = outcome != OverrideSkillCheckOutcome.Expired;
            bool nextClockwise = directionWillToggle
                ? !_activeChallenge.Clockwise
                : _activeChallenge.Clockwise;

            switch (outcome)
            {
                case OverrideSkillCheckOutcome.Success:
                    SetResult("LINK CONFIRMED", Config != null ? Config.AccentColor : Color.cyan);
                    PlaySuccessFeedback(animated);
                    break;

                case OverrideSkillCheckOutcome.Expired:
                    SetResult("WINDOW EXPIRED", Config != null ? Config.NegativeColor : Color.red);
                    PlayFailureFeedback(animated);
                    break;

                default:
                    SetResult("SYNC MISSED", Config != null ? Config.NegativeColor : Color.red);
                    PlayFailureFeedback(animated);
                    break;
            }

            SetDirection(nextClockwise, _activeChallenge.DurationSeconds, true);
        }

        public override void ResetImmediate()
        {
            _challengeActive = false;
            KillTweens();
            SetIndicatorPosition(0f);
            ConfigureSuccessZone(0f, 0.22f);

            if (resultFlashImage != null && Config != null)
                resultFlashImage.color = WithAlpha(Config.NegativeColor, 0f);

            if (impactRippleImage != null && Config != null)
                impactRippleImage.color = WithAlpha(Config.AccentColor, 0f);

            if (centerPulseRoot != null)
                centerPulseRoot.localScale = Vector3.one;

            if (timingRingRoot != null)
            {
                timingRingRoot.localScale = Vector3.one;
                timingRingRoot.localRotation = Quaternion.identity;
            }

            if (ambientHaloRoot != null)
                ambientHaloRoot.localScale = Vector3.one;

            if (successZoneRoot != null)
                successZoneRoot.localScale = Vector3.one;

            if (needlePivot != null)
                needlePivot.localScale = Vector3.one;

            if (resultText != null)
            {
                resultText.rectTransform.localScale = Vector3.one;
                resultText.rectTransform.anchoredPosition = _resultLabelRestPosition;
            }

            SetResult("SYNC WINDOW", Config != null ? Config.SecondaryColor : Color.yellow);
            SetDirection(true, 2f, false);
        }

        public void SetEditorPreview(
            float indicatorPosition,
            float successZoneStart,
            float successZoneSize,
            bool clockwise = true,
            float durationSeconds = 2f)
        {
            ConfigureSuccessZone(successZoneStart, successZoneSize);
            SetIndicatorPosition(indicatorPosition);
            UpdateNeedleProximity(
                indicatorPosition >= successZoneStart &&
                indicatorPosition <= successZoneStart + successZoneSize);
            SetResult("SYNC WINDOW", Config != null ? Config.SecondaryColor : Color.yellow);
            SetDirection(clockwise, durationSeconds, false);
        }

        private void ConfigureSuccessZone(float start, float size)
        {
            if (successZoneRoot != null)
                successZoneRoot.localRotation = Quaternion.Euler(0f, 0f, -start * 360f);

            if (successZoneImage != null)
                successZoneImage.fillAmount = Mathf.Clamp01(size);

            if (successZoneGlowImage != null)
                successZoneGlowImage.fillAmount = Mathf.Clamp01(size);
        }

        private void SetIndicatorPosition(float normalizedPosition)
        {
            if (needlePivot != null)
                needlePivot.localRotation =
                    Quaternion.Euler(0f, 0f, -Mathf.Repeat(normalizedPosition, 1f) * 360f);
        }

        private void UpdateNeedleProximity(bool insideSuccessZone)
        {
            if (Config == null)
                return;

            if (needleImage != null)
                needleImage.color = insideSuccessZone ? Config.SecondaryColor : Config.PrimaryColor;

            if (needleGlowImage != null)
            {
                needleGlowImage.color = WithAlpha(
                    insideSuccessZone ? Config.SecondaryColor : Config.AccentColor,
                    insideSuccessZone ? 0.62f : 0.18f);
            }
        }

        private void SetDirection(bool clockwise, float durationSeconds, bool showNext)
        {
            if (directionText != null)
            {
                float speed = durationSeconds > 0f ? 1f / durationSeconds : 0f;
                string prefix = showNext ? "NEXT " : string.Empty;
                directionText.text = $"{prefix}{(clockwise ? "CW" : "CCW")}  /  {speed:0.00} RPS";
                directionText.color = Config != null ? Config.AccentColor : Color.cyan;
            }

            if (directionBadgeRoot == null)
                return;

            _directionTween?.Kill();
            directionBadgeRoot.localScale = Vector3.one;
            if (showNext)
            {
                _directionTween = directionBadgeRoot.DOPunchScale(
                        Vector3.one * 0.18f,
                        0.28f,
                        7,
                        0.7f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        private void PlayRoundStart(bool clockwise)
        {
            if (Config == null)
                return;

            PlayImpactRipple(Config.AccentColor, 0.34f);

            if (timingRingRoot != null)
            {
                _ringMotionTween?.Kill();
                timingRingRoot.localRotation = Quaternion.identity;
                _ringMotionTween = timingRingRoot.DOPunchRotation(
                        new Vector3(0f, 0f, clockwise ? -7f : 7f),
                        0.3f,
                        7,
                        0.55f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (needlePivot != null)
            {
                needlePivot.localScale = Vector3.one;
                needlePivot.DOPunchScale(Vector3.one * 0.12f, 0.22f, 6, 0.65f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        private void StartAmbientMotion()
        {
            _zonePulseTween?.Kill();
            _haloTween?.Kill();

            if (successZoneRoot != null)
            {
                successZoneRoot.localScale = Vector3.one;
                _zonePulseTween = successZoneRoot.DOScale(1.045f, 0.42f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (ambientHaloRoot != null)
            {
                ambientHaloRoot.localScale = Vector3.one;
                _haloTween = ambientHaloRoot.DOScale(1.035f, 0.72f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        private void PlaySuccessFeedback(bool animated)
        {
            if (!animated || Config == null)
                return;

            PlayImpactRipple(Config.AccentColor, 0.78f);

            _resultTween?.Kill();
            if (resultFlashImage != null)
            {
                resultFlashImage.color = WithAlpha(Config.AccentColor, 0f);
                _resultTween = resultFlashImage.DOFade(0.34f, Config.ResultFlashDuration)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            _centerPulseTween?.Kill();
            if (centerPulseRoot != null)
            {
                centerPulseRoot.localScale = Vector3.one;
                _centerPulseTween = centerPulseRoot.DOPunchScale(
                        Vector3.one * (Config.SuccessPulseScale - 1f + 0.06f),
                        Config.FeedbackAnimationDuration,
                        7,
                        0.55f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (timingRingRoot != null)
            {
                timingRingRoot.localScale = Vector3.one;
                timingRingRoot.DOPunchScale(Vector3.one * 0.085f, 0.34f, 7, 0.65f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            _zoneImpactTween?.Kill();
            if (successZoneRoot != null)
            {
                successZoneRoot.localScale = Vector3.one;
                _zoneImpactTween = successZoneRoot.DOPunchScale(
                        Vector3.one * 0.14f,
                        0.32f,
                        7,
                        0.65f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            PlayResultLabelFeedback(true);
        }

        private void PlayFailureFeedback(bool animated)
        {
            if (!animated || Config == null)
                return;

            PlayImpactRipple(Config.NegativeColor, 0.72f);

            _resultTween?.Kill();
            if (resultFlashImage != null)
            {
                resultFlashImage.color = WithAlpha(Config.NegativeColor, 0f);
                _resultTween = resultFlashImage.DOFade(0.68f, Config.ResultFlashDuration * 1.25f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (timingRingRoot != null)
            {
                _ringMotionTween?.Kill();
                timingRingRoot.localRotation = Quaternion.identity;
                _ringMotionTween = timingRingRoot.DOShakeRotation(
                        0.34f,
                        new Vector3(0f, 0f, 8f),
                        20,
                        90f,
                        false)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            _needleImpactTween?.Kill();
            if (needlePivot != null)
            {
                needlePivot.localScale = Vector3.one;
                _needleImpactTween = needlePivot.DOPunchScale(
                        new Vector3(0.16f, -0.08f, 0f),
                        0.28f,
                        9,
                        0.75f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            PlayResultLabelFeedback(false);
        }

        private void PlayResultLabelFeedback(bool success)
        {
            if (resultText == null)
                return;

            _resultLabelTween?.Kill();
            RectTransform rect = resultText.rectTransform;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = _resultLabelRestPosition;

            _resultLabelTween = success
                ? rect.DOPunchScale(Vector3.one * 0.2f, 0.3f, 7, 0.65f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                : rect.DOShakeAnchorPos(0.32f, new Vector2(12f, 3f), 20, 90f, false, true)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void PlayImpactRipple(Color color, float peakAlpha)
        {
            if (impactRippleRoot == null || impactRippleImage == null)
                return;

            _impactSequence?.Kill();
            impactRippleRoot.localScale = Vector3.one * 0.76f;
            impactRippleImage.color = WithAlpha(color, peakAlpha);

            _impactSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _impactSequence.Join(impactRippleRoot.DOScale(1.14f, 0.32f).SetEase(Ease.OutCubic));
            _impactSequence.Join(impactRippleImage.DOFade(0f, 0.32f).SetEase(Ease.OutQuad));
        }

        private void SetResult(string value, Color color)
        {
            if (resultText == null)
                return;

            resultText.text = value;
            resultText.color = color;
        }

        private void KillTweens()
        {
            _resultTween?.Kill();
            _centerPulseTween?.Kill();
            _zonePulseTween?.Kill();
            _haloTween?.Kill();
            _ringMotionTween?.Kill();
            _directionTween?.Kill();
            _resultLabelTween?.Kill();
            _zoneImpactTween?.Kill();
            _needleImpactTween?.Kill();
            _impactSequence?.Kill();
            _resultTween = null;
            _centerPulseTween = null;
            _zonePulseTween = null;
            _haloTween = null;
            _ringMotionTween = null;
            _directionTween = null;
            _resultLabelTween = null;
            _zoneImpactTween = null;
            _needleImpactTween = null;
            _impactSequence = null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void OnDisable()
        {
            _challengeActive = false;
            KillTweens();
        }
    }
}
