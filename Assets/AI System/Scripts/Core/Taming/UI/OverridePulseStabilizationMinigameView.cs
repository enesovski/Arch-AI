using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Artifika.UI.Override
{
    [DisallowMultipleComponent]
    public sealed class OverridePulseStabilizationMinigameView : OverrideMinigameView
    {
        [Header("Layout")]
        [SerializeField] private RectTransform arenaRoot;
        [SerializeField] private RectTransform safeZoneRect;
        [SerializeField] private RectTransform pulseRingRect;
        [SerializeField] private RectTransform cursorRect;

        [Header("Graphics")]
        [SerializeField] private Image arenaTrackImage;
        [SerializeField] private Image safeZoneImage;
        [SerializeField] private Image pulseRingImage;
        [SerializeField] private Image cursorGlowImage;
        [SerializeField] private Image cursorImage;
        [SerializeField] private Image resultFlashImage;
        [SerializeField] private TextMeshProUGUI stateText;

        private Vector2 _normalizedPosition;
        private Vector2 _normalizedVelocity;
        private Vector2 _driftSeedDirection = Vector2.right;
        private float _noiseTime;
        private bool _sessionActive;
        private bool _pulseActive;
        private double _pulseStartedAt;
        private Tween _feedbackTween;
        private Tween _cursorTween;

        public override OverrideMinigameType Type => OverrideMinigameType.PulseStabilization;
        public bool IsPulseActive => _pulseActive;
        public float NormalizedDistance => _normalizedPosition.magnitude;

        public override void Configure(OverrideSkillCheckUIConfigSO config)
        {
            base.Configure(config);

            float arenaDiameter = config.StabilizationArenaDiameter;
            float movementRadius = Mathf.Max(1f, (arenaDiameter - config.StabilizationCursorDiameter) * 0.5f);
            float safeDiameter = movementRadius * config.StabilizationSuccessRadius * 2f;

            if (arenaRoot != null)
                arenaRoot.sizeDelta = Vector2.one * arenaDiameter;

            if (safeZoneRect != null)
                safeZoneRect.sizeDelta = Vector2.one * safeDiameter;

            if (pulseRingRect != null)
                pulseRingRect.sizeDelta = Vector2.one * arenaDiameter;

            if (cursorRect != null)
                cursorRect.sizeDelta = Vector2.one * config.StabilizationCursorDiameter;

            if (arenaTrackImage != null)
                arenaTrackImage.color = WithAlpha(config.PrimaryColor, 0.16f);

            if (safeZoneImage != null)
                safeZoneImage.color = WithAlpha(config.AccentColor, 0.2f);

            if (pulseRingImage != null)
                pulseRingImage.color = config.SecondaryColor;

            if (cursorGlowImage != null)
                cursorGlowImage.color = WithAlpha(config.AccentColor, 0.34f);

            if (cursorImage != null)
                cursorImage.color = config.PrimaryColor;

            if (resultFlashImage != null)
                resultFlashImage.color = WithAlpha(config.NegativeColor, 0f);

            UpdateCursorVisual();
        }

        public void BeginSession()
        {
            ResetImmediate();
            _sessionActive = true;
            _noiseTime = Random.Range(0f, 50f);
            _driftSeedDirection = Random.insideUnitCircle.normalized;
            if (_driftSeedDirection.sqrMagnitude < 0.01f)
                _driftSeedDirection = Vector2.right;

            _normalizedPosition = Random.insideUnitCircle * 0.18f;
            UpdateCursorVisual();
            SetState("KEEP SIGNAL CENTERED", Config != null ? Config.PrimaryColor : Color.white);
        }

        public void BeginPulse(double now)
        {
            if (!_sessionActive)
                BeginSession();

            _pulseStartedAt = now;
            _pulseActive = true;

            if (pulseRingRect != null)
                pulseRingRect.localScale = Vector3.one;

            if (pulseRingImage != null && Config != null)
                pulseRingImage.color = WithAlpha(Config.SecondaryColor, 0.48f);

            SetState("INCOMING PULSE", Config != null ? Config.SecondaryColor : Color.yellow);
        }

        public bool Tick(
            double now,
            float unscaledDeltaTime,
            Vector2 mouseDelta,
            Vector2 gamepadStick,
            out OverrideSkillCheckOutcome outcome,
            out float evaluatedDistance)
        {
            outcome = OverrideSkillCheckOutcome.Failed;
            evaluatedDistance = NormalizedDistance;

            if (!_sessionActive || Config == null)
                return false;

            float deltaTime = Mathf.Clamp(unscaledDeltaTime, 0f, 0.05f);
            SimulateCursor(deltaTime, mouseDelta, gamepadStick);
            UpdateCursorVisual();

            if (!_pulseActive)
                return false;

            float duration = Mathf.Max(0.25f, Config.StabilizationPulseDuration);
            float progress = Mathf.Clamp01((float)((now - _pulseStartedAt) / duration));
            UpdatePulseVisual(progress);

            if (progress < 1f)
                return false;

            _pulseActive = false;
            evaluatedDistance = NormalizedDistance;
            outcome = evaluatedDistance <= Config.StabilizationSuccessRadius
                ? OverrideSkillCheckOutcome.Success
                : OverrideSkillCheckOutcome.Failed;
            return true;
        }

        public override void ShowOutcome(OverrideSkillCheckOutcome outcome, bool animated)
        {
            bool success = outcome == OverrideSkillCheckOutcome.Success;
            Color color = success
                ? (Config != null ? Config.AccentColor : Color.cyan)
                : (Config != null ? Config.NegativeColor : Color.red);

            SetState(success ? "SIGNAL STABLE" : "SIGNAL LOST", color);

            if (!animated || Config == null)
                return;

            _feedbackTween?.Kill();
            _cursorTween?.Kill();

            if (resultFlashImage != null)
            {
                resultFlashImage.color = WithAlpha(color, 0f);
                _feedbackTween = resultFlashImage.DOFade(success ? 0.24f : 0.42f, Config.ResultFlashDuration)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (cursorRect != null)
            {
                cursorRect.localScale = Vector3.one;
                _cursorTween = cursorRect.DOPunchScale(
                        Vector3.one * (success ? 0.18f : 0.1f),
                        Config.FeedbackAnimationDuration,
                        success ? 6 : 10,
                        success ? 0.55f : 1f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        public override void ResetImmediate()
        {
            _sessionActive = false;
            _pulseActive = false;
            _normalizedPosition = Vector2.zero;
            _normalizedVelocity = Vector2.zero;
            _feedbackTween?.Kill();
            _cursorTween?.Kill();
            _feedbackTween = null;
            _cursorTween = null;

            if (pulseRingRect != null)
                pulseRingRect.localScale = Vector3.one;

            if (cursorRect != null)
                cursorRect.localScale = Vector3.one;

            if (pulseRingImage != null && Config != null)
                pulseRingImage.color = WithAlpha(Config.SecondaryColor, 0f);

            if (resultFlashImage != null && Config != null)
                resultFlashImage.color = WithAlpha(Config.NegativeColor, 0f);

            UpdateCursorVisual();
            SetState("KEEP SIGNAL CENTERED", Config != null ? Config.PrimaryColor : Color.white);
        }

        public void SetEditorPreview(Vector2 normalizedPosition, float pulseProgress)
        {
            _normalizedPosition = Vector2.ClampMagnitude(normalizedPosition, 1f);
            UpdateCursorVisual();
            UpdatePulseVisual(Mathf.Clamp01(pulseProgress));
            SetState("INCOMING PULSE", Config != null ? Config.SecondaryColor : Color.yellow);
        }

        private void SimulateCursor(float deltaTime, Vector2 mouseDelta, Vector2 gamepadStick)
        {
            Vector2 mouseImpulse = Vector2.ClampMagnitude(mouseDelta, 90f) * Config.StabilizationMouseImpulse;
            _normalizedVelocity += mouseImpulse;
            _normalizedVelocity += Vector2.ClampMagnitude(gamepadStick, 1f) *
                                   (Config.StabilizationStickAcceleration * deltaTime);

            _noiseTime += deltaTime * 0.72f;
            Vector2 noise = new(
                Mathf.PerlinNoise(_noiseTime, 7.13f) * 2f - 1f,
                Mathf.PerlinNoise(13.71f, _noiseTime) * 2f - 1f);

            Vector2 outward = _normalizedPosition.sqrMagnitude > 0.0025f
                ? _normalizedPosition.normalized
                : _driftSeedDirection;

            Vector2 destabilization =
                outward * Config.StabilizationDriftAcceleration +
                noise * Config.StabilizationNoiseAcceleration;
            _normalizedVelocity += destabilization * deltaTime;

            float damping = Mathf.Exp(-Config.StabilizationDamping * deltaTime);
            _normalizedVelocity *= damping;
            _normalizedVelocity = Vector2.ClampMagnitude(
                _normalizedVelocity,
                Config.StabilizationMaxSpeed);
            _normalizedPosition += _normalizedVelocity * deltaTime;

            if (_normalizedPosition.sqrMagnitude <= 1f)
                return;

            Vector2 normal = _normalizedPosition.normalized;
            _normalizedPosition = normal;
            float outwardSpeed = Vector2.Dot(_normalizedVelocity, normal);
            if (outwardSpeed > 0f)
            {
                _normalizedVelocity -= normal * outwardSpeed *
                    (1f + Config.StabilizationBoundaryBounce);
            }
        }

        private void UpdateCursorVisual()
        {
            if (Config == null)
                return;

            float movementRadius = Mathf.Max(
                1f,
                (Config.StabilizationArenaDiameter - Config.StabilizationCursorDiameter) * 0.5f);

            if (cursorRect != null)
                cursorRect.anchoredPosition = _normalizedPosition * movementRadius;

            float safeRadius = Mathf.Max(0.001f, Config.StabilizationSuccessRadius);
            float proximity = 1f - Mathf.InverseLerp(safeRadius * 0.55f, safeRadius * 1.8f, NormalizedDistance);
            Color cursorColor = Color.Lerp(Config.NegativeColor, Config.PrimaryColor, proximity);

            if (cursorImage != null)
                cursorImage.color = cursorColor;

            if (cursorGlowImage != null)
                cursorGlowImage.color = WithAlpha(Color.Lerp(Config.NegativeColor, Config.AccentColor, proximity), 0.34f);
        }

        private void UpdatePulseVisual(float progress)
        {
            if (pulseRingRect == null || Config == null)
                return;

            float eased = progress * progress * (3f - 2f * progress);
            float targetScale = Mathf.Max(0.04f, Config.StabilizationSuccessRadius * 0.35f);
            float scale = Mathf.Lerp(1f, targetScale, eased);
            pulseRingRect.localScale = Vector3.one * scale;

            if (pulseRingImage != null)
            {
                float alpha = Mathf.Lerp(0.42f, 1f, eased);
                pulseRingImage.color = WithAlpha(Config.SecondaryColor, alpha);
            }
        }

        private void SetState(string value, Color color)
        {
            if (stateText == null)
                return;

            stateText.text = value;
            stateText.color = color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void OnDisable()
        {
            _pulseActive = false;
            _feedbackTween?.Kill();
            _cursorTween?.Kill();
        }
    }
}
