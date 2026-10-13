using UnityEngine;
using UnityEngine.InputSystem;

namespace Artifika.UI.Override
{
    /// <summary>
    /// Preview-scene-only simulation. It does not use NetworkManager or the gameplay entry flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverrideSkillCheckSlicePreview : MonoBehaviour
    {
        [SerializeField] private OverrideSkillCheckHUDView view;
        [SerializeField] private OverrideMinigameType selectedMinigame =
            OverrideMinigameType.TimingRing;
        [SerializeField, Range(2, 10)] private int requiredChecks = 5;

        [Header("Timing Ring Preview")]
        [SerializeField, Range(0.15f, 2f)] private float timingNeedleRevolutionsPerSecond = 0.55f;
        [SerializeField, Range(0.05f, 0.9f)] private float timingSuccessZoneSize = 0.22f;
        [SerializeField] private bool continueNeedleImmediatelyAfterCheck;

        [Header("Shared Preview Flow")]
        [SerializeField, Min(1f)] private float mockStunDurationSeconds = 12f;
        [SerializeField, Min(0.05f)] private float failedCheckStunTimePenaltySeconds = 1.5f;
        [SerializeField, Min(0f)] private float delayBetweenChecks = 0.55f;
        [SerializeField, Min(0f)] private float restartDelay = 1.25f;

        private LocalOverrideSkillCheckData _timingChallenge;
        private bool _roundActive;
        private uint _nextChallengeId;
        private int _completedChecks;
        private double _nextChallengeTime;
        private bool _restartPending;
        private double _mockStunEndsAt;
        private float _timingNeedleStartPosition;
        private bool _timingNeedleClockwise = true;

        public void SetView(OverrideSkillCheckHUDView value)
        {
            view = value;
        }

        private void Start()
        {
            RestartPreview();
        }

        private void Update()
        {
            if (view == null)
                return;

            HandlePreviewControls();

            double now = Time.unscaledTimeAsDouble;
            view.Tick(now);

            if (!_restartPending && now >= _mockStunEndsAt)
            {
                SimulateSessionLoss();
                return;
            }

            if (selectedMinigame == OverrideMinigameType.PulseStabilization)
                UpdatePulseStabilization(now);

            if (_restartPending && now >= _nextChallengeTime)
            {
                RestartPreview();
                return;
            }

            if (!_roundActive && !_restartPending && now >= _nextChallengeTime)
                IssueRound(now);
        }

        private void UpdatePulseStabilization(double now)
        {
            OverridePulseStabilizationMinigameView pulseView =
                view.GetMinigame<OverridePulseStabilizationMinigameView>();
            if (pulseView == null)
                return;

            Vector2 mouseDelta = Mouse.current != null
                ? Mouse.current.delta.ReadValue()
                : Vector2.zero;
            Vector2 gamepadStick = Gamepad.current != null
                ? Gamepad.current.rightStick.ReadValue()
                : Vector2.zero;

            if (pulseView.Tick(
                    now,
                    Time.unscaledDeltaTime,
                    mouseDelta,
                    gamepadStick,
                    out OverrideSkillCheckOutcome outcome,
                    out float evaluatedDistance))
            {
                ResolveRound(outcome, evaluatedDistance, now);
            }
        }

        private void HandlePreviewControls()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame &&
                selectedMinigame != OverrideMinigameType.TimingRing)
            {
                selectedMinigame = OverrideMinigameType.TimingRing;
                RestartPreview();
            }

            if (keyboard.digit2Key.wasPressedThisFrame &&
                selectedMinigame != OverrideMinigameType.PulseStabilization)
            {
                selectedMinigame = OverrideMinigameType.PulseStabilization;
                RestartPreview();
            }

            if (keyboard.rKey.wasPressedThisFrame)
                RestartPreview();

            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                requiredChecks = Mathf.Max(2, requiredChecks - 1);
                RestartPreview();
            }

            if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                requiredChecks = Mathf.Min(10, requiredChecks + 1);
                RestartPreview();
            }

            if (keyboard.minusKey.wasPressedThisFrame)
            {
                timingNeedleRevolutionsPerSecond = Mathf.Max(
                    0.15f,
                    timingNeedleRevolutionsPerSecond - 0.1f);
                if (selectedMinigame == OverrideMinigameType.TimingRing)
                    RestartPreview();
            }

            if (keyboard.equalsKey.wasPressedThisFrame)
            {
                timingNeedleRevolutionsPerSecond = Mathf.Min(
                    2f,
                    timingNeedleRevolutionsPerSecond + 0.1f);
                if (selectedMinigame == OverrideMinigameType.TimingRing)
                    RestartPreview();
            }

            if (keyboard.pKey.wasPressedThisFrame)
                continueNeedleImmediatelyAfterCheck = !continueNeedleImmediatelyAfterCheck;

            if (keyboard.lKey.wasPressedThisFrame && !_restartPending)
            {
                SimulateSessionLoss();
                return;
            }

            if (selectedMinigame != OverrideMinigameType.TimingRing ||
                !keyboard.spaceKey.wasPressedThisFrame ||
                !_roundActive)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            float position = _timingChallenge.GetNormalizedIndicatorPosition(now);
            bool challengeStarted = now >= _timingChallenge.StartsAtServerTime;
            bool success = challengeStarted && _timingChallenge.Contains(position);
            ResolveRound(
                success ? OverrideSkillCheckOutcome.Success : OverrideSkillCheckOutcome.Failed,
                position,
                now);
        }

        private void RestartPreview()
        {
            _completedChecks = 0;
            _nextChallengeId = 1;
            _roundActive = false;
            _restartPending = false;
            _nextChallengeTime = Time.unscaledTimeAsDouble + 0.5d;
            _timingNeedleStartPosition = 0f;
            _timingNeedleClockwise = true;

            double now = Time.unscaledTimeAsDouble;
            _mockStunEndsAt = now + mockStunDurationSeconds;
            view?.ShowSession(
                requiredChecks,
                selectedMinigame,
                _mockStunEndsAt,
                mockStunDurationSeconds);

            if (selectedMinigame == OverrideMinigameType.PulseStabilization)
                view?.GetMinigame<OverridePulseStabilizationMinigameView>()?.BeginSession();
        }

        private void IssueRound(double now, bool startImmediately = false)
        {
            _roundActive = true;

            if (selectedMinigame == OverrideMinigameType.PulseStabilization)
            {
                view.GetMinigame<OverridePulseStabilizationMinigameView>()?.BeginPulse(now);
                return;
            }

            float zoneStart = Random.Range(
                0.05f,
                Mathf.Max(0.05f, 0.95f - timingSuccessZoneSize));
            float duration = 1f / Mathf.Max(0.01f, timingNeedleRevolutionsPerSecond);
            _timingChallenge = new LocalOverrideSkillCheckData(
                1,
                _nextChallengeId++,
                startImmediately ? now : now + 0.35d,
                duration,
                zoneStart,
                timingSuccessZoneSize,
                _timingNeedleStartPosition,
                _timingNeedleClockwise);
            view.BeginChallenge(_timingChallenge);
        }

        private void ResolveRound(
            OverrideSkillCheckOutcome outcome,
            float evaluatedValue,
            double now)
        {
            if (!_roundActive)
                return;

            _roundActive = false;
            if (outcome == OverrideSkillCheckOutcome.Success)
                _completedChecks++;

            float stunTimePenalty = 0f;
            if (outcome != OverrideSkillCheckOutcome.Success)
            {
                float remainingStunTime = Mathf.Max(0f, (float)(_mockStunEndsAt - now));
                stunTimePenalty = Mathf.Min(remainingStunTime, failedCheckStunTimePenaltySeconds);
                _mockStunEndsAt -= stunTimePenalty;
            }

            if (selectedMinigame == OverrideMinigameType.TimingRing)
            {
                _timingNeedleStartPosition = Mathf.Repeat(evaluatedValue, 1f);
                if (outcome != OverrideSkillCheckOutcome.Expired)
                    _timingNeedleClockwise = !_timingNeedleClockwise;

                view.ResolveChallenge(new LocalOverrideSkillCheckResult(
                    1,
                    _timingChallenge.ChallengeId,
                    outcome,
                    evaluatedValue,
                    _completedChecks,
                    requiredChecks,
                    _mockStunEndsAt,
                    stunTimePenalty),
                    now);
            }
            else
            {
                view.ResolveCurrentMinigameCheck(
                    outcome,
                    _completedChecks,
                    requiredChecks);
                view.ApplyStunTimePenalty(
                    _mockStunEndsAt,
                    stunTimePenalty,
                    now);
            }

            if (_mockStunEndsAt <= now)
            {
                SimulateSessionLoss();
                return;
            }

            if (_completedChecks >= requiredChecks)
            {
                view.ShowSessionOutcome(true);
                _restartPending = true;
                _nextChallengeTime = now + restartDelay;
                return;
            }

            if (selectedMinigame == OverrideMinigameType.TimingRing &&
                continueNeedleImmediatelyAfterCheck)
            {
                IssueRound(now, true);
                return;
            }

            _nextChallengeTime = now + delayBetweenChecks;
        }

        private void SimulateSessionLoss()
        {
            _roundActive = false;
            _restartPending = true;
            _nextChallengeTime = Time.unscaledTimeAsDouble + restartDelay;
            view.ShowSessionOutcome(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            requiredChecks = Mathf.Clamp(requiredChecks, 2, 10);
            timingNeedleRevolutionsPerSecond = Mathf.Clamp(
                timingNeedleRevolutionsPerSecond,
                0.15f,
                2f);
            timingSuccessZoneSize = Mathf.Clamp(timingSuccessZoneSize, 0.05f, 0.9f);
            mockStunDurationSeconds = Mathf.Max(1f, mockStunDurationSeconds);
            failedCheckStunTimePenaltySeconds = Mathf.Max(
                0.05f,
                failedCheckStunTimePenaltySeconds);
            delayBetweenChecks = Mathf.Max(0f, delayBetweenChecks);
            restartDelay = Mathf.Max(0f, restartDelay);
        }
#endif
    }
}
