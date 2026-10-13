using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ReachProgressBar = Michsky.UI.Reach.ProgressBar;

namespace Artifika.UI.Override
{
    [DisallowMultipleComponent]
    public sealed class OverrideSkillCheckHUDView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private OverrideSkillCheckUIConfigSO config;

        [Header("Shared Layout")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Stun Timer")]
        [SerializeField] private ReachProgressBar stunTimeProgressBar;

        [Header("Terminal Feedback")]
        [SerializeField] private CanvasGroup terminalFeedbackCanvasGroup;
        [SerializeField] private RectTransform terminalFeedbackRoot;
        [SerializeField] private Image terminalFeedbackBackdrop;
        [SerializeField] private Image[] terminalFeedbackAccents;
        [SerializeField] private TextMeshProUGUI terminalFeedbackText;

        [Header("Audio Feedback")]
        [SerializeField] private OverrideSkillCheckAudioFeedback audioFeedback;

        [Header("Substitutable Minigames")]
        [SerializeField] private OverrideMinigameType activeMinigameType =
            OverrideMinigameType.TimingRing;
        [SerializeField] private OverrideMinigameView[] minigameViews;

        private int _requiredChecks;
        private int _completedChecks;
        private Tween _visibilityTween;
        private Tween _progressValueTween;
        private Tween _progressFeedbackTween;
        private Tween _stunValueTween;
        private Tween _stunPenaltyFlashTween;
        private Tween _stunPenaltyShakeTween;
        private Sequence _terminalFeedbackSequence;
        private double _stunEndsAtServerTime;
        private float _stunDurationSeconds;
        private float _stunPenaltyFlash;
        private Color _stunBaseColor = Color.white;
        private bool _hasStunTiming;
        private bool _stunBaseColorCaptured;
        private bool _stunValueAnimating;
        private float _displayedProgressPercent;

        public OverrideSkillCheckUIConfigSO Config => config;
        public OverrideMinigameType ActiveMinigameType => activeMinigameType;
        public float TerminalFeedbackDuration => config != null
            ? config.TerminalFeedbackDuration
            : 0.95f;

        private void Awake()
        {
            CaptureStunBaseColor();
            ApplyConfiguration();
            ApplyMinigameVisibility();

            if (Application.isPlaying)
                HideImmediate();
        }

        public void SetConfig(OverrideSkillCheckUIConfigSO value)
        {
            config = value;
            ApplyConfiguration();
        }

        public void SetMinigameViews(params OverrideMinigameView[] views)
        {
            minigameViews = views;
            ApplyConfiguration();
            ApplyMinigameVisibility();
        }

        public T GetMinigame<T>() where T : OverrideMinigameView
        {
            if (minigameViews == null)
                return null;

            for (int i = 0; i < minigameViews.Length; i++)
            {
                if (minigameViews[i] is T typedView)
                    return typedView;
            }

            return null;
        }

        public void SelectMinigame(OverrideMinigameType type, bool reset = true)
        {
            OverrideMinigameView previous = FindMinigame(activeMinigameType);
            if (reset && previous != null)
                previous.ResetImmediate();

            activeMinigameType = type;
            ApplyMinigameVisibility();

            OverrideMinigameView active = FindMinigame(activeMinigameType);
            if (reset && active != null)
                active.ResetImmediate();

            UpdateModeLabels();
        }

        public void ShowSession(int requiredSuccessfulChecks, bool animated = true)
        {
            ShowSession(requiredSuccessfulChecks, activeMinigameType, animated);
        }

        public void ShowSession(
            int requiredSuccessfulChecks,
            OverrideMinigameType minigameType,
            bool animated = true)
        {
            ShowSession(requiredSuccessfulChecks, minigameType, 0d, 0f, animated);
        }

        public void ShowSession(
            int requiredSuccessfulChecks,
            OverrideMinigameType minigameType,
            double stunEndsAtServerTime,
            float stunDurationSeconds,
            bool animated = true)
        {
            SelectMinigame(minigameType);
            _requiredChecks = Mathf.Max(1, requiredSuccessfulChecks);
            _completedChecks = 0;

            ConfigureStunTimer(stunEndsAtServerTime, stunDurationSeconds);
            SetCompletedChecks(0, false);
            ResetTerminalFeedback();
            audioFeedback?.BeginSession();
            Show(animated);
        }

        public void BeginChallenge(LocalOverrideSkillCheckData challenge)
        {
            if (activeMinigameType != OverrideMinigameType.TimingRing)
                SelectMinigame(OverrideMinigameType.TimingRing);

            GetMinigame<OverrideTimingRingMinigameView>()?.BeginChallenge(challenge);
        }

        public void Tick(double serverTime)
        {
            if (activeMinigameType == OverrideMinigameType.TimingRing)
                GetMinigame<OverrideTimingRingMinigameView>()?.Tick(serverTime);

            UpdateStunTimer(serverTime);
        }

        public void ResolveChallenge(
            LocalOverrideSkillCheckResult result,
            double serverTime,
            bool animated = true)
        {
            GetMinigame<OverrideTimingRingMinigameView>()?.Resolve(
                result.EvaluatedIndicatorPosition,
                result.Outcome,
                animated);
            SetCompletedChecks(result.CompletedSuccessfulChecks, animated);
            PlayProgressFeedback(result.Outcome, animated);
            ApplyStunTimePenalty(
                result.StunEndsAtServerTime,
                result.StunTimePenaltySeconds,
                serverTime,
                animated);
            audioFeedback?.PlayCheckResult(result.Outcome);
        }

        public void ResolveCurrentMinigameCheck(
            OverrideSkillCheckOutcome outcome,
            int completedChecks,
            int requiredChecks,
            bool animated = true)
        {
            _requiredChecks = Mathf.Max(1, requiredChecks);
            FindMinigame(activeMinigameType)?.ShowOutcome(outcome, animated);
            SetCompletedChecks(completedChecks, animated);
            PlayProgressFeedback(outcome, animated);
            audioFeedback?.PlayCheckResult(outcome);
        }

        public void ShowSessionOutcome(bool completed, bool animated = true)
        {
            audioFeedback?.PlaySessionEnd(completed);
            if (!completed)
                PlayStunFailureFeedback(animated);
            _terminalFeedbackSequence?.Kill();

            Color textColor = config == null
                ? (terminalFeedbackText != null ? terminalFeedbackText.color : Color.white)
                : completed
                    ? config.TerminalSuccessTextColor
                    : config.TerminalFailureTextColor;
            Color backgroundColor = config == null
                ? (terminalFeedbackBackdrop != null ? terminalFeedbackBackdrop.color : Color.white)
                : completed
                    ? config.TerminalSuccessBackgroundColor
                    : config.TerminalFailureBackgroundColor;

            if (terminalFeedbackText != null)
            {
                if (config != null)
                    terminalFeedbackText.text = config.GetTerminalFeedbackText(completed);
                terminalFeedbackText.color = textColor;
            }

            if (terminalFeedbackBackdrop != null)
            {
                float backdropAlpha = config == null
                    ? terminalFeedbackBackdrop.color.a
                    : completed
                        ? config.TerminalSuccessBackdropAlpha
                        : config.TerminalFailureBackdropAlpha;
                terminalFeedbackBackdrop.color = WithAlpha(backgroundColor, backdropAlpha);
            }

            if (terminalFeedbackAccents != null)
            {
                for (int i = 0; i < terminalFeedbackAccents.Length; i++)
                {
                    if (terminalFeedbackAccents[i] != null)
                        terminalFeedbackAccents[i].color = backgroundColor;
                }
            }

            if (terminalFeedbackCanvasGroup == null)
                return;

            terminalFeedbackCanvasGroup.alpha = 1f;
            terminalFeedbackCanvasGroup.interactable = false;
            terminalFeedbackCanvasGroup.blocksRaycasts = false;

            if (!animated || terminalFeedbackRoot == null)
            {
                if (terminalFeedbackRoot != null)
                    terminalFeedbackRoot.localScale = Vector3.one;
                return;
            }

            terminalFeedbackRoot.localScale = Vector3.one * 0.82f;
            _terminalFeedbackSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _terminalFeedbackSequence.Append(
                terminalFeedbackRoot.DOScale(1.06f, 0.18f).SetEase(Ease.OutBack));
            _terminalFeedbackSequence.Append(
                terminalFeedbackRoot.DOScale(1f, 0.12f).SetEase(Ease.OutCubic));
            _terminalFeedbackSequence.Join(
                terminalFeedbackRoot.DOPunchRotation(
                    new Vector3(0f, 0f, completed ? 2f : 5f),
                    0.3f,
                    completed ? 4 : 9,
                    0.7f));
        }

        public void SetCompletedChecks(int completedChecks, bool animated)
        {
            _completedChecks = Mathf.Clamp(completedChecks, 0, Mathf.Max(1, _requiredChecks));

            SetProgressPercent(CalculateProgressPercent(), animated);
        }

        private float CalculateProgressPercent()
        {
            return (float)_completedChecks / Mathf.Max(1, _requiredChecks) * 100f;
        }

        private void SetProgressPercent(float targetPercent, bool animated)
        {
            targetPercent = Mathf.Clamp(targetPercent, 0f, 100f);
            _progressValueTween?.Kill();
            _progressValueTween = null;

            if (progressText == null)
            {
                _displayedProgressPercent = targetPercent;
                return;
            }

            bool shouldCountUp = animated && targetPercent > _displayedProgressPercent;
            float duration = config != null ? config.CompletionCountDuration : 0f;
            if (!shouldCountUp || duration <= 0f)
            {
                SetDisplayedProgressPercent(targetPercent);
                return;
            }

            _progressValueTween = DOTween.To(
                    () => _displayedProgressPercent,
                    SetDisplayedProgressPercent,
                    targetPercent,
                    duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    SetDisplayedProgressPercent(targetPercent);
                    _progressValueTween = null;
                });
        }

        private void SetDisplayedProgressPercent(float percent)
        {
            _displayedProgressPercent = percent;
            if (progressText != null)
                progressText.SetText("{0:0}%", Mathf.RoundToInt(percent));
        }

        public void Show(bool animated = true)
        {
            if (rootCanvasGroup == null)
                return;

            _visibilityTween?.Kill();
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;

            if (!animated || config == null || config.FadeDuration <= 0f)
            {
                rootCanvasGroup.alpha = 1f;
                if (contentRoot != null)
                    contentRoot.localScale = Vector3.one;
                return;
            }

            rootCanvasGroup.alpha = 0f;
            if (contentRoot != null)
                contentRoot.localScale = Vector3.one * 0.96f;

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Join(rootCanvasGroup.DOFade(1f, config.FadeDuration));
            if (contentRoot != null)
                sequence.Join(contentRoot.DOScale(1f, config.FadeDuration).SetEase(Ease.OutCubic));
            _visibilityTween = sequence;
        }

        public void Hide(bool animated = true)
        {
            _visibilityTween?.Kill();

            if (rootCanvasGroup == null)
                return;

            if (!animated || config == null || config.FadeDuration <= 0f)
            {
                HideImmediate();
                return;
            }

            _visibilityTween = rootCanvasGroup.DOFade(0f, config.FadeDuration)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void HideImmediate()
        {
            _visibilityTween?.Kill();
            _progressValueTween?.Kill();
            _progressFeedbackTween?.Kill();
            _progressValueTween = null;
            _progressFeedbackTween = null;
            KillStunFeedback();
            _terminalFeedbackSequence?.Kill();

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.blocksRaycasts = false;
                rootCanvasGroup.interactable = false;
            }

            if (contentRoot != null)
                contentRoot.localScale = Vector3.one;

            ResetProgressTextVisuals();
            ResetTerminalFeedback();
        }

        public void SetEditorPreview(
            int requiredChecks,
            int completedChecks,
            OverrideMinigameType minigameType,
            float timingIndicatorPosition = 0.31f,
            float timingSuccessZoneStart = 0.58f,
            float timingSuccessZoneSize = 0.22f)
        {
            _requiredChecks = Mathf.Max(1, requiredChecks);
            SetCompletedChecks(completedChecks, false);

            if (rootCanvasGroup != null)
                rootCanvasGroup.alpha = 1f;

            SelectMinigame(minigameType);

            if (minigameType == OverrideMinigameType.TimingRing)
            {
                GetMinigame<OverrideTimingRingMinigameView>()?.SetEditorPreview(
                    timingIndicatorPosition,
                    timingSuccessZoneStart,
                    timingSuccessZoneSize);
            }
            else
            {
                GetMinigame<OverridePulseStabilizationMinigameView>()?.SetEditorPreview(
                    new Vector2(0.18f, -0.12f),
                    0.48f);
            }
        }

        private void ApplyConfiguration()
        {
            if (config == null)
                return;

            if (contentRoot != null)
                contentRoot.sizeDelta = Vector2.one * config.RootSize;

            SetTextColor(titleText, config.PrimaryColor);
            SetTextColor(hintText, WithAlpha(config.PrimaryColor, config.HintTextAlpha));
            SetTextColor(progressText, config.CompletionColor);
            SetTextFont(titleText, config.TitleFont);
            SetTextFont(hintText, config.HintFont);
            SetTextFont(progressText, config.CompletionFont);
            SetTextFont(terminalFeedbackText, config.TerminalFont);

            if (terminalFeedbackBackdrop != null)
            {
                terminalFeedbackBackdrop.color = WithAlpha(
                    config.TerminalFailureBackgroundColor,
                    config.TerminalFailureBackdropAlpha);
            }

            if (terminalFeedbackText != null)
                terminalFeedbackText.color = config.TerminalFailureTextColor;

            if (terminalFeedbackAccents != null)
            {
                for (int i = 0; i < terminalFeedbackAccents.Length; i++)
                {
                    if (terminalFeedbackAccents[i] != null)
                        terminalFeedbackAccents[i].color = config.TerminalFailureBackgroundColor;
                }
            }

            if (minigameViews != null)
            {
                for (int i = 0; i < minigameViews.Length; i++)
                    minigameViews[i]?.Configure(config);
            }

            UpdateModeLabels();
        }

        private void ApplyMinigameVisibility()
        {
            if (minigameViews == null)
                return;

            for (int i = 0; i < minigameViews.Length; i++)
            {
                OverrideMinigameView minigame = minigameViews[i];
                if (minigame != null)
                    minigame.SetVisible(minigame.Type == activeMinigameType);
            }
        }

        private void UpdateModeLabels()
        {
            if (config == null)
                return;

            if (titleText != null)
                titleText.text = config.GetTitleText();

            if (hintText != null)
                hintText.text = config.GetHintText(activeMinigameType);
        }

        private OverrideMinigameView FindMinigame(OverrideMinigameType type)
        {
            if (minigameViews == null)
                return null;

            for (int i = 0; i < minigameViews.Length; i++)
            {
                OverrideMinigameView view = minigameViews[i];
                if (view != null && view.Type == type)
                    return view;
            }

            return null;
        }

        private void ConfigureStunTimer(double endsAtServerTime, float durationSeconds)
        {
            KillStunFeedback();
            _stunEndsAtServerTime = endsAtServerTime;
            _stunDurationSeconds = Mathf.Max(0f, durationSeconds);
            _hasStunTiming = _stunEndsAtServerTime > 0d && _stunDurationSeconds > 0f;

            if (stunTimeProgressBar == null)
                return;

            CaptureStunBaseColor();
            stunTimeProgressBar.minValue = 0f;
            stunTimeProgressBar.maxValue = Mathf.Max(0.01f, _stunDurationSeconds);
            stunTimeProgressBar.SetValue(_hasStunTiming ? _stunDurationSeconds : 0f);
            SetStunBarColor(_stunBaseColor);
        }

        private void UpdateStunTimer(double serverTime)
        {
            if (!_hasStunTiming || stunTimeProgressBar == null)
                return;

            float remainingSeconds = Mathf.Clamp(
                (float)(_stunEndsAtServerTime - serverTime),
                0f,
                _stunDurationSeconds);
            if (!_stunValueAnimating)
                stunTimeProgressBar.SetValue(remainingSeconds);

            UpdateStunTimerColor(remainingSeconds);
        }

        public void ApplyStunTimePenalty(
            double updatedEndsAtServerTime,
            float penaltySeconds,
            double serverTime,
            bool animated = true)
        {
            if (updatedEndsAtServerTime > 0d)
                _stunEndsAtServerTime = updatedEndsAtServerTime;

            if (penaltySeconds <= 0f || stunTimeProgressBar == null)
                return;

            float remainingSeconds = Mathf.Clamp(
                (float)(_stunEndsAtServerTime - serverTime),
                0f,
                _stunDurationSeconds);

            _stunValueTween?.Kill();
            _stunValueTween = null;
            _stunValueAnimating = false;

            if (!animated)
            {
                stunTimeProgressBar.SetValue(remainingSeconds);
            }
            else
            {
                _stunValueAnimating = true;
                _stunValueTween = DOTween.To(
                        () => stunTimeProgressBar.currentValue,
                        stunTimeProgressBar.SetValue,
                        remainingSeconds,
                        0.24f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                    .OnKill(() => _stunValueAnimating = false)
                    .OnComplete(() => _stunValueAnimating = false);
            }

            PlayStunFailureFeedback(animated);
        }

        private void PlayStunFailureFeedback(bool animated)
        {
            if (!animated || stunTimeProgressBar == null)
                return;

            _stunPenaltyFlashTween?.Kill();
            _stunPenaltyFlash = 1f;
            _stunPenaltyFlashTween = DOTween.To(
                    () => _stunPenaltyFlash,
                    value => _stunPenaltyFlash = value,
                    0f,
                    0.48f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (stunTimeProgressBar.transform is not RectTransform rect)
                return;

            _stunPenaltyShakeTween?.Kill();
            rect.localRotation = Quaternion.identity;
            _stunPenaltyShakeTween = rect.DOShakeRotation(
                    0.34f,
                    new Vector3(0f, 0f, 5f),
                    18,
                    90f,
                    true)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => rect.localRotation = Quaternion.identity);
        }

        private void UpdateStunTimerColor(float remainingSeconds)
        {
            CaptureStunBaseColor();
            float normalized = _stunDurationSeconds > 0f
                ? Mathf.Clamp01(remainingSeconds / _stunDurationSeconds)
                : 0f;
            float warning = Mathf.InverseLerp(0.45f, 0.05f, normalized);
            float pulseSpeed = Mathf.Lerp(5f, 12f, warning);
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f;
            float warningFlash = warning * Mathf.Lerp(0.55f, 1f, pulse);
            float negativeBlend = Mathf.Max(warningFlash, _stunPenaltyFlash);
            Color negativeColor = config != null ? config.NegativeColor : _stunBaseColor;
            SetStunBarColor(Color.Lerp(_stunBaseColor, negativeColor, negativeBlend));
        }

        private void CaptureStunBaseColor()
        {
            if (_stunBaseColorCaptured || stunTimeProgressBar?.barImage == null)
                return;

            _stunBaseColor = stunTimeProgressBar.barImage.color;
            _stunBaseColorCaptured = true;
        }

        private void SetStunBarColor(Color color)
        {
            if (stunTimeProgressBar?.barImage != null)
                stunTimeProgressBar.barImage.color = color;
        }

        private void KillStunFeedback()
        {
            _stunValueAnimating = false;
            _stunValueTween?.Kill();
            _stunPenaltyFlashTween?.Kill();
            _stunPenaltyShakeTween?.Kill();
            _stunValueTween = null;
            _stunPenaltyFlashTween = null;
            _stunPenaltyShakeTween = null;
            _stunPenaltyFlash = 0f;

            if (stunTimeProgressBar != null)
                stunTimeProgressBar.transform.localRotation = Quaternion.identity;
        }

        private void PlayProgressFeedback(OverrideSkillCheckOutcome outcome, bool animated)
        {
            if (!animated || progressText == null || config == null)
                return;

            _progressFeedbackTween?.Kill();
            RectTransform progressRect = progressText.rectTransform;
            progressRect.localScale = Vector3.one;
            progressRect.localRotation = Quaternion.identity;
            progressText.color = config.CompletionColor;

            if (outcome == OverrideSkillCheckOutcome.Success)
            {
                float flashDuration = config.CompletionFlashDuration;
                if (flashDuration <= 0f)
                    return;

                Sequence sequence = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                sequence.Append(
                    progressRect.DOScale(config.CompletionPulseScale, flashDuration)
                        .SetEase(Ease.OutCubic));
                sequence.Join(progressText.DOColor(config.CompletionFlashColor, flashDuration));
                sequence.Append(
                    progressRect.DOScale(1f, flashDuration * 1.4f)
                        .SetEase(Ease.OutBack));
                sequence.Join(
                    progressText.DOColor(config.CompletionColor, flashDuration * 1.4f));
                sequence.OnComplete(() => _progressFeedbackTween = null);
                _progressFeedbackTween = sequence;
                return;
            }

            if (config.CompletionFailureShakeDuration <= 0f ||
                config.CompletionFailureShakeAngle <= 0f)
            {
                return;
            }

            _progressFeedbackTween = progressRect.DOShakeRotation(
                    config.CompletionFailureShakeDuration,
                    new Vector3(0f, 0f, config.CompletionFailureShakeAngle),
                    18,
                    90f,
                    true)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void ResetTerminalFeedback()
        {
            _terminalFeedbackSequence?.Kill();

            if (terminalFeedbackCanvasGroup != null)
            {
                terminalFeedbackCanvasGroup.alpha = 0f;
                terminalFeedbackCanvasGroup.interactable = false;
                terminalFeedbackCanvasGroup.blocksRaycasts = false;
            }

            if (terminalFeedbackRoot != null)
            {
                terminalFeedbackRoot.localScale = Vector3.one;
                terminalFeedbackRoot.localRotation = Quaternion.identity;
            }
        }

        private static void SetTextColor(TextMeshProUGUI text, Color color)
        {
            if (text != null)
                text.color = color;
        }

        private static void SetTextFont(TextMeshProUGUI text, TMP_FontAsset font)
        {
            if (text != null && font != null)
                text.font = font;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void ResetProgressTextVisuals()
        {
            if (progressText == null)
                return;

            progressText.rectTransform.localScale = Vector3.one;
            progressText.rectTransform.localRotation = Quaternion.identity;
            if (config != null)
                progressText.color = config.CompletionColor;
        }

        private void OnDisable()
        {
            _visibilityTween?.Kill();
            _progressValueTween?.Kill();
            _progressFeedbackTween?.Kill();
            _progressValueTween = null;
            _progressFeedbackTween = null;
            KillStunFeedback();
            _terminalFeedbackSequence?.Kill();

            ResetProgressTextVisuals();
        }
    }
}
