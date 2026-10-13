using Michsky.UI.Reach;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Artifika.UI.Override
{
    [CreateAssetMenu(
        fileName = "SO_OverrideSkillCheckUIConfig",
        menuName = "UI/Override/Skill Check UI Config")]
    public sealed class OverrideSkillCheckUIConfigSO : ScriptableObject
    {
        [Header("Reach UI Palette")]
        [SerializeField] private UIManager reachTheme;

        [Header("Layout")]
        [SerializeField, Min(200f)] private float rootSize = 720f;
        [SerializeField, Min(100f)] private float timingRingDiameter = 350f;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.18f;
        [FormerlySerializedAs("barFillDuration")]
        [SerializeField, Min(0f)] private float feedbackAnimationDuration = 0.28f;
        [SerializeField, Min(0f)] private float resultFlashDuration = 0.12f;
        [SerializeField, Min(1f)] private float successPulseScale = 1.08f;
        [SerializeField, Min(0.25f)] private float terminalFeedbackDuration = 0.95f;

        [Header("Completion Percentage")]
        [SerializeField, Min(0f)] private float completionCountDuration = 0.28f;
        [SerializeField, Min(0f)] private float completionFlashDuration = 0.12f;
        [SerializeField, Min(1f)] private float completionPulseScale = 1.08f;
        [SerializeField, Min(0f)] private float completionFailureShakeDuration = 0.28f;
        [SerializeField, Min(0f)] private float completionFailureShakeAngle = 3.5f;

        [Header("Text Presentation")]
        [SerializeField, Range(0f, 1f)] private float hintTextAlpha = 0.62f;
        [SerializeField, Range(0f, 1f)] private float terminalSuccessBackdropAlpha = 0.2f;
        [SerializeField, Range(0f, 1f)] private float terminalFailureBackdropAlpha = 0.26f;

        [Header("Localized Text")]
        [SerializeField] private string localizationTable = "UI";
        [SerializeField] private string titleLocalizationKey = "UI_OverrideSkillCheck_Title";
        [SerializeField] private string titleFallback = "ROBOT OVERRIDE";
        [SerializeField] private string timingHintLocalizationKey = "UI_OverrideSkillCheck_TimingHint";
        [SerializeField] private string timingHintFallback = "HOLD ATTACK  /  PRESS SPACE";
        [SerializeField] private string stabilizationHintLocalizationKey =
            "UI_OverrideSkillCheck_StabilizationHint";
        [SerializeField] private string stabilizationHintFallback = "HOLD ATTACK  /  STEER SIGNAL";
        [SerializeField] private string successFeedbackLocalizationKey =
            "UI_OverrideSkillCheck_Success";
        [SerializeField] private string successFeedbackFallback = "Success";
        [SerializeField] private string failedFeedbackLocalizationKey =
            "UI_OverrideSkillCheck_Failed";
        [SerializeField] private string failedFeedbackFallback = "Failed";

        [Header("Pulse Stabilization")]
        [SerializeField, Min(120f)] private float stabilizationArenaDiameter = 350f;
        [SerializeField, Range(0.05f, 0.75f)] private float stabilizationSuccessRadius = 0.24f;
        [SerializeField, Min(8f)] private float stabilizationCursorDiameter = 30f;
        [SerializeField, Min(0.25f)] private float stabilizationPulseDuration = 1.65f;
        [SerializeField, Min(0f)] private float stabilizationMouseImpulse = 0.035f;
        [SerializeField, Min(0f)] private float stabilizationStickAcceleration = 5f;
        [SerializeField, Min(0f)] private float stabilizationDriftAcceleration = 0.72f;
        [SerializeField, Min(0f)] private float stabilizationNoiseAcceleration = 0.48f;
        [SerializeField, Min(0f)] private float stabilizationDamping = 1.75f;
        [SerializeField, Min(0.1f)] private float stabilizationMaxSpeed = 1.55f;
        [SerializeField, Range(0f, 1f)] private float stabilizationBoundaryBounce = 0.3f;

        [Header("Background Color")]
        [SerializeField] private bool backgroundUsesPalette = true;
        [SerializeField] private PaletteColor backgroundPaletteColor = PaletteColor.Background;
        [SerializeField] private Color backgroundColor = new(0.098f, 0.137f, 0.176f, 1f);

        [Header("Primary Color")]
        [SerializeField] private bool primaryUsesPalette = true;
        [SerializeField] private PaletteColor primaryPaletteColor = PaletteColor.Primary;
        [SerializeField] private Color primaryColor = Color.white;

        [Header("Accent Color")]
        [SerializeField] private bool accentUsesPalette = true;
        [SerializeField] private PaletteColor accentPaletteColor = PaletteColor.Accent;
        [SerializeField] private Color accentColor = new(0.82f, 0.655f, 1f, 1f);

        [Header("Secondary Color")]
        [SerializeField] private bool secondaryUsesPalette = true;
        [SerializeField] private PaletteColor secondaryPaletteColor = PaletteColor.Secondary;
        [SerializeField] private Color secondaryColor = new(1f, 0.575f, 0f, 1f);

        [Header("Negative Color")]
        [SerializeField] private bool negativeUsesPalette = true;
        [SerializeField] private PaletteColor negativePaletteColor = PaletteColor.Negative;
        [SerializeField] private Color negativeColor = new(1f, 0.294f, 0.294f, 1f);

        [Header("Completion Text Color")]
        [SerializeField] private bool completionColorUsesPalette = true;
        [SerializeField] private PaletteColor completionPaletteColor = PaletteColor.Primary;
        [SerializeField] private Color completionOverrideColor = Color.white;

        [Header("Completion Flash Color")]
        [SerializeField] private bool completionFlashUsesPalette = true;
        [SerializeField] private PaletteColor completionFlashPaletteColor = PaletteColor.Accent;
        [SerializeField] private Color completionFlashOverrideColor =
            new(0.82f, 0.655f, 1f, 1f);

        [Header("Terminal Success Text Color")]
        [SerializeField] private bool terminalSuccessTextUsesPalette = true;
        [SerializeField] private PaletteColor terminalSuccessTextPaletteColor =
            PaletteColor.Primary;
        [SerializeField] private Color terminalSuccessTextOverrideColor = Color.white;

        [Header("Terminal Failure Text Color")]
        [SerializeField] private bool terminalFailureTextUsesPalette = true;
        [SerializeField] private PaletteColor terminalFailureTextPaletteColor =
            PaletteColor.Primary;
        [SerializeField] private Color terminalFailureTextOverrideColor = Color.white;

        [Header("Terminal Success Background Color")]
        [SerializeField] private bool terminalSuccessBackgroundUsesPalette;
        [SerializeField] private PaletteColor terminalSuccessBackgroundPaletteColor =
            PaletteColor.Accent;
        [SerializeField] private Color terminalSuccessBackgroundOverrideColor =
            new(0.28f, 0.84f, 0.48f, 1f);

        [Header("Terminal Failure Background Color")]
        [SerializeField] private bool terminalFailureBackgroundUsesPalette = true;
        [SerializeField] private PaletteColor terminalFailureBackgroundPaletteColor =
            PaletteColor.Negative;
        [SerializeField] private Color terminalFailureBackgroundOverrideColor =
            new(1f, 0.294f, 0.294f, 1f);

        [Header("Typography")]
        [SerializeField] private bool titleFontUsesPalette = true;
        [SerializeField] private PaletteFont titlePaletteFont = PaletteFont.SemiBold;
        [SerializeField] private TMP_FontAsset titleOverrideFont;
        [SerializeField] private bool hintFontUsesPalette = true;
        [SerializeField] private PaletteFont hintPaletteFont = PaletteFont.Medium;
        [SerializeField] private TMP_FontAsset hintOverrideFont;
        [SerializeField] private bool completionFontUsesPalette = true;
        [SerializeField] private PaletteFont completionPaletteFont = PaletteFont.Bold;
        [SerializeField] private TMP_FontAsset completionOverrideFont;
        [SerializeField] private bool terminalFontUsesPalette = true;
        [SerializeField] private PaletteFont terminalPaletteFont = PaletteFont.Bold;
        [SerializeField] private TMP_FontAsset terminalOverrideFont;

        public float RootSize => rootSize;
        public float TimingRingDiameter => timingRingDiameter;
        public float FadeDuration => fadeDuration;
        public float FeedbackAnimationDuration => feedbackAnimationDuration;
        public float ResultFlashDuration => resultFlashDuration;
        public float SuccessPulseScale => successPulseScale;
        public float TerminalFeedbackDuration => terminalFeedbackDuration;
        public float CompletionCountDuration => completionCountDuration;
        public float CompletionFlashDuration => completionFlashDuration;
        public float CompletionPulseScale => completionPulseScale;
        public float CompletionFailureShakeDuration => completionFailureShakeDuration;
        public float CompletionFailureShakeAngle => completionFailureShakeAngle;
        public float HintTextAlpha => hintTextAlpha;
        public float TerminalSuccessBackdropAlpha => terminalSuccessBackdropAlpha;
        public float TerminalFailureBackdropAlpha => terminalFailureBackdropAlpha;
        public float StabilizationArenaDiameter => stabilizationArenaDiameter;
        public float StabilizationSuccessRadius => stabilizationSuccessRadius;
        public float StabilizationCursorDiameter => stabilizationCursorDiameter;
        public float StabilizationPulseDuration => stabilizationPulseDuration;
        public float StabilizationMouseImpulse => stabilizationMouseImpulse;
        public float StabilizationStickAcceleration => stabilizationStickAcceleration;
        public float StabilizationDriftAcceleration => stabilizationDriftAcceleration;
        public float StabilizationNoiseAcceleration => stabilizationNoiseAcceleration;
        public float StabilizationDamping => stabilizationDamping;
        public float StabilizationMaxSpeed => stabilizationMaxSpeed;
        public float StabilizationBoundaryBounce => stabilizationBoundaryBounce;

        public Color BackgroundColor => ResolveColor(
            backgroundUsesPalette,
            backgroundPaletteColor,
            backgroundColor);
        public Color PrimaryColor => ResolveColor(
            primaryUsesPalette,
            primaryPaletteColor,
            primaryColor);
        public Color AccentColor => ResolveColor(
            accentUsesPalette,
            accentPaletteColor,
            accentColor);
        public Color SecondaryColor => ResolveColor(
            secondaryUsesPalette,
            secondaryPaletteColor,
            secondaryColor);
        public Color NegativeColor => ResolveColor(
            negativeUsesPalette,
            negativePaletteColor,
            negativeColor);
        public Color CompletionColor => ResolveColor(
            completionColorUsesPalette,
            completionPaletteColor,
            completionOverrideColor);
        public Color CompletionFlashColor => ResolveColor(
            completionFlashUsesPalette,
            completionFlashPaletteColor,
            completionFlashOverrideColor);
        public Color TerminalSuccessTextColor => ResolveColor(
            terminalSuccessTextUsesPalette,
            terminalSuccessTextPaletteColor,
            terminalSuccessTextOverrideColor);
        public Color TerminalFailureTextColor => ResolveColor(
            terminalFailureTextUsesPalette,
            terminalFailureTextPaletteColor,
            terminalFailureTextOverrideColor);
        public Color TerminalSuccessBackgroundColor => ResolveColor(
            terminalSuccessBackgroundUsesPalette,
            terminalSuccessBackgroundPaletteColor,
            terminalSuccessBackgroundOverrideColor);
        public Color TerminalFailureBackgroundColor => ResolveColor(
            terminalFailureBackgroundUsesPalette,
            terminalFailureBackgroundPaletteColor,
            terminalFailureBackgroundOverrideColor);
        public TMP_FontAsset TitleFont => ResolveFont(
            titleFontUsesPalette,
            titlePaletteFont,
            titleOverrideFont);
        public TMP_FontAsset HintFont => ResolveFont(
            hintFontUsesPalette,
            hintPaletteFont,
            hintOverrideFont);
        public TMP_FontAsset CompletionFont => ResolveFont(
            completionFontUsesPalette,
            completionPaletteFont,
            completionOverrideFont);
        public TMP_FontAsset TerminalFont => ResolveFont(
            terminalFontUsesPalette,
            terminalPaletteFont,
            terminalOverrideFont);

        public void SetReachTheme(UIManager theme)
        {
            reachTheme = theme;
        }

        public string GetTitleText()
        {
            return LocalizationService.Get(localizationTable, titleLocalizationKey, titleFallback);
        }

        public string GetHintText(OverrideMinigameType minigameType)
        {
            return minigameType == OverrideMinigameType.TimingRing
                ? LocalizationService.Get(
                    localizationTable,
                    timingHintLocalizationKey,
                    timingHintFallback)
                : LocalizationService.Get(
                    localizationTable,
                    stabilizationHintLocalizationKey,
                    stabilizationHintFallback);
        }

        public string GetTerminalFeedbackText(bool completed)
        {
            return completed
                ? LocalizationService.Get(
                    localizationTable,
                    successFeedbackLocalizationKey,
                    successFeedbackFallback)
                : LocalizationService.Get(
                    localizationTable,
                    failedFeedbackLocalizationKey,
                    failedFeedbackFallback);
        }

        private Color ResolveColor(bool usePalette, PaletteColor color, Color overrideColor)
        {
            if (!usePalette || reachTheme == null)
                return overrideColor;

            return color switch
            {
                PaletteColor.Background => reachTheme.backgroundColor,
                PaletteColor.Primary => reachTheme.primaryColor,
                PaletteColor.Accent => reachTheme.accentColor,
                PaletteColor.Secondary => reachTheme.secondaryColor,
                PaletteColor.Negative => reachTheme.negativeColor,
                _ => overrideColor,
            };
        }

        private TMP_FontAsset ResolveFont(
            bool usePalette,
            PaletteFont font,
            TMP_FontAsset overrideFont)
        {
            if (!usePalette || reachTheme == null)
                return overrideFont;

            return font switch
            {
                PaletteFont.Light => reachTheme.fontLight,
                PaletteFont.Regular => reachTheme.fontRegular,
                PaletteFont.Medium => reachTheme.fontMedium,
                PaletteFont.SemiBold => reachTheme.fontSemiBold,
                PaletteFont.Bold => reachTheme.fontBold,
                PaletteFont.Custom => reachTheme.customFont,
                _ => overrideFont,
            };
        }

        private enum PaletteColor
        {
            Background,
            Primary,
            Accent,
            Secondary,
            Negative,
        }

        private enum PaletteFont
        {
            Light,
            Regular,
            Medium,
            SemiBold,
            Bold,
            Custom,
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            rootSize = Mathf.Max(200f, rootSize);
            timingRingDiameter = Mathf.Clamp(timingRingDiameter, 100f, rootSize);
            fadeDuration = Mathf.Max(0f, fadeDuration);
            feedbackAnimationDuration = Mathf.Max(0f, feedbackAnimationDuration);
            resultFlashDuration = Mathf.Max(0f, resultFlashDuration);
            successPulseScale = Mathf.Max(1f, successPulseScale);
            terminalFeedbackDuration = Mathf.Max(0.25f, terminalFeedbackDuration);
            completionCountDuration = Mathf.Max(0f, completionCountDuration);
            completionFlashDuration = Mathf.Max(0f, completionFlashDuration);
            completionPulseScale = Mathf.Max(1f, completionPulseScale);
            completionFailureShakeDuration = Mathf.Max(0f, completionFailureShakeDuration);
            completionFailureShakeAngle = Mathf.Max(0f, completionFailureShakeAngle);
            hintTextAlpha = Mathf.Clamp01(hintTextAlpha);
            terminalSuccessBackdropAlpha = Mathf.Clamp01(terminalSuccessBackdropAlpha);
            terminalFailureBackdropAlpha = Mathf.Clamp01(terminalFailureBackdropAlpha);
            stabilizationArenaDiameter = Mathf.Clamp(stabilizationArenaDiameter, 120f, rootSize);
            stabilizationSuccessRadius = Mathf.Clamp(stabilizationSuccessRadius, 0.05f, 0.75f);
            stabilizationCursorDiameter = Mathf.Clamp(
                stabilizationCursorDiameter,
                8f,
                stabilizationArenaDiameter * 0.35f);
            stabilizationPulseDuration = Mathf.Max(0.25f, stabilizationPulseDuration);
            stabilizationMouseImpulse = Mathf.Max(0f, stabilizationMouseImpulse);
            stabilizationStickAcceleration = Mathf.Max(0f, stabilizationStickAcceleration);
            stabilizationDriftAcceleration = Mathf.Max(0f, stabilizationDriftAcceleration);
            stabilizationNoiseAcceleration = Mathf.Max(0f, stabilizationNoiseAcceleration);
            stabilizationDamping = Mathf.Max(0f, stabilizationDamping);
            stabilizationMaxSpeed = Mathf.Max(0.1f, stabilizationMaxSpeed);
            stabilizationBoundaryBounce = Mathf.Clamp01(stabilizationBoundaryBounce);
        }
#endif
    }
}
