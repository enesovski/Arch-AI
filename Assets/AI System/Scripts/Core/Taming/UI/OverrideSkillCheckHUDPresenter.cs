using System.Collections;
using Artifika.UI;
using UnityEngine;

namespace Artifika.UI.Override
{
    [DisallowMultipleComponent]
    public sealed class OverrideSkillCheckHUDPresenter : MonoBehaviour, IUIPanel
    {
        [SerializeField] private UIPanelId panelId = UIPanelId.Override;
        [SerializeField] private OverrideSkillCheckHUDView view;

        private TamingController _boundController;
        private LocalOverrideSessionData _activeSession;
        private LocalOverrideSkillCheckData _activeChallenge;
        private bool _statePanelRequested;
        private bool _sessionActive;
        private bool _sessionPresented;
        private bool _hasActiveChallenge;
        private bool _terminalFeedbackActive;
        private uint _presentedChallengeId;
        private int _completedChecks;
        private Coroutine _terminalHideCoroutine;

        public UIPanelId PanelId => panelId;

        public void SetView(OverrideSkillCheckHUDView value)
        {
            view = value;
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void Update()
        {
            if (_boundController == null)
                TryBind();

            if (_boundController != null && view != null && _sessionPresented)
                view.Tick(_boundController.LocalServerTime);
        }

        public IEnumerator Show()
        {
            _statePanelRequested = true;
            TryPresentSession();
            yield break;
        }

        public IEnumerator Hide()
        {
            _statePanelRequested = false;

            if (_sessionActive || _terminalFeedbackActive)
                yield break;

            _sessionPresented = false;
            _presentedChallengeId = 0;
            view?.Hide();
            yield break;
        }

        private void TryBind()
        {
            TamingController controller = TamingController.Instance;
            if (controller == null || controller == _boundController)
                return;

            Unbind();
            _boundController = controller;
            _boundController.LocalOverrideStarted += HandleOverrideStarted;
            _boundController.LocalSkillCheckStarted += HandleSkillCheckStarted;
            _boundController.LocalSkillCheckResolved += HandleSkillCheckResolved;
            _boundController.LocalOverrideEnded += HandleOverrideEnded;
        }

        private void HandleOverrideStarted(LocalOverrideSessionData session)
        {
            StopTerminalHideRoutine();
            _activeSession = session;
            _sessionActive = true;
            _sessionPresented = false;
            _hasActiveChallenge = false;
            _terminalFeedbackActive = false;
            _presentedChallengeId = 0;
            _completedChecks = 0;
            TryPresentSession();
        }

        private void HandleSkillCheckStarted(LocalOverrideSkillCheckData challenge)
        {
            if (!_sessionActive || challenge.SessionId != _activeSession.SessionId)
                return;

            _activeChallenge = challenge;
            _hasActiveChallenge = true;
            TryPresentSession();
        }

        private void HandleSkillCheckResolved(LocalOverrideSkillCheckResult result)
        {
            if (!_sessionActive || result.SessionId != _activeSession.SessionId)
                return;

            _completedChecks = result.CompletedSuccessfulChecks;
            TryPresentSession();

            if (_sessionPresented && view != null)
                view.ResolveChallenge(result, _boundController.LocalServerTime);

            if (_hasActiveChallenge && _activeChallenge.ChallengeId == result.ChallengeId)
                _hasActiveChallenge = false;

            if (_presentedChallengeId == result.ChallengeId)
                _presentedChallengeId = 0;
        }

        private void HandleOverrideEnded(ulong sessionId, OverrideEndReason reason)
        {
            if (!_sessionActive || sessionId != _activeSession.SessionId)
                return;

            bool wasPresented = _sessionPresented && view != null;
            bool completed = reason == OverrideEndReason.Completed;
            _sessionActive = false;
            _hasActiveChallenge = false;
            _presentedChallengeId = 0;
            _completedChecks = 0;

            if (!wasPresented)
            {
                _sessionPresented = false;
                view?.Hide();
                return;
            }

            _terminalFeedbackActive = true;
            view.ShowSessionOutcome(completed);
            StopTerminalHideRoutine();
            _terminalHideCoroutine = StartCoroutine(HideAfterTerminalFeedback());
        }

        private IEnumerator HideAfterTerminalFeedback()
        {
            yield return new WaitForSecondsRealtime(view != null
                ? view.TerminalFeedbackDuration
                : 0.95f);

            _terminalFeedbackActive = false;
            _sessionPresented = false;
            _terminalHideCoroutine = null;
            view?.Hide();
        }

        private void StopTerminalHideRoutine()
        {
            if (_terminalHideCoroutine == null)
                return;

            StopCoroutine(_terminalHideCoroutine);
            _terminalHideCoroutine = null;
        }

        private void TryPresentSession()
        {
            if (!_statePanelRequested || !_sessionActive || view == null)
                return;

            if (!_sessionPresented)
            {
                view.ShowSession(
                    _activeSession.RequiredSuccessfulChecks,
                    OverrideMinigameType.TimingRing,
                    _activeSession.StunEndsAtServerTime,
                    _activeSession.StunDurationSeconds);
                view.Tick(_boundController.LocalServerTime);
                view.SetCompletedChecks(_completedChecks, false);
                _sessionPresented = true;
            }

            if (_hasActiveChallenge && _presentedChallengeId != _activeChallenge.ChallengeId)
            {
                view.BeginChallenge(_activeChallenge);
                _presentedChallengeId = _activeChallenge.ChallengeId;
            }
        }

        private void Unbind()
        {
            if (_boundController == null)
                return;

            _boundController.LocalOverrideStarted -= HandleOverrideStarted;
            _boundController.LocalSkillCheckStarted -= HandleSkillCheckStarted;
            _boundController.LocalSkillCheckResolved -= HandleSkillCheckResolved;
            _boundController.LocalOverrideEnded -= HandleOverrideEnded;
            _boundController = null;
        }

        private void OnDisable()
        {
            StopTerminalHideRoutine();
            Unbind();
            _statePanelRequested = false;
            _sessionActive = false;
            _sessionPresented = false;
            _hasActiveChallenge = false;
            _terminalFeedbackActive = false;
            _presentedChallengeId = 0;
            view?.HideImmediate();
        }
    }
}
