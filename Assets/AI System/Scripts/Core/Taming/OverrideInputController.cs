using Artifika.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owner-local input bridge for an active override session.
/// The server remains authoritative; this component only reports held/released input
/// and the ID of the currently displayed skill check.
/// </summary>
[DisallowMultipleComponent]
public sealed class OverrideInputController : MonoBehaviour
{
    private TamingController _tamingController;
    private PlayerInputs _inputs;
    private InputAction _skillCheckAction;
    private bool _subscribed;
    private bool _sessionActive;
    private ulong _sessionId;
    private bool _skillCheckActive;
    private uint _skillCheckId;
    private float _heartbeatInterval;
    private float _nextHeartbeatTime;

    public void Initialize(TamingController tamingController)
    {
        _tamingController = tamingController;

        InputBootstrap.OnLocalReady -= HandleInputReady;
        InputBootstrap.OnLocalReady += HandleInputReady;

        if (InputBootstrap.Local != null)
            BindInput(InputBootstrap.Local);
    }

    public void BeginSession(ulong sessionId, float heartbeatInterval)
    {
        _sessionId = sessionId;
        _heartbeatInterval = Mathf.Max(0.05f, heartbeatInterval);
        _sessionActive = true;
        _nextHeartbeatTime = Time.unscaledTime;

        if (!IsAttackHeld())
        {
            CancelActiveSession();
            return;
        }

        SendHeartbeat();
    }

    public void EndSession(ulong sessionId)
    {
        if (!_sessionActive || sessionId != _sessionId)
            return;

        ClearSession();
    }

    public void BeginSkillCheck(ulong sessionId, uint challengeId)
    {
        if (!_sessionActive || sessionId != _sessionId)
            return;

        _skillCheckId = challengeId;
        _skillCheckActive = true;
    }

    public void EndSkillCheck(ulong sessionId, uint challengeId)
    {
        if (!_sessionActive || sessionId != _sessionId ||
            !_skillCheckActive || challengeId != _skillCheckId)
        {
            return;
        }

        ClearSkillCheck();
    }

    private void Update()
    {
        if (!_sessionActive || Time.unscaledTime < _nextHeartbeatTime)
            return;

        if (!IsAttackHeld())
        {
            CancelActiveSession();
            return;
        }

        SendHeartbeat();
    }

    private void HandleInputReady(InputBootstrap bootstrap)
    {
        BindInput(bootstrap);
    }

    private void BindInput(InputBootstrap bootstrap)
    {
        if (bootstrap == null || !bootstrap.IsInitialized || bootstrap.Actions == null)
            return;

        UnbindInput();

        _inputs = bootstrap.Actions;
        _inputs.Player.Attack.canceled += HandleAttackReleased;

        _skillCheckAction = _inputs.Player.Get().FindAction("SkillCheck", throwIfNotFound: false);
        if (_skillCheckAction != null)
        {
            _skillCheckAction.performed += HandleSkillCheck;
        }
        else
        {
            GameLog.Error("[OverrideInputController] Player/SkillCheck input action is missing.");
        }

        _subscribed = true;
    }

    private void UnbindInput()
    {
        if (!_subscribed || _inputs == null)
            return;

        _inputs.Player.Attack.canceled -= HandleAttackReleased;

        if (_skillCheckAction != null)
            _skillCheckAction.performed -= HandleSkillCheck;

        _skillCheckAction = null;
        _subscribed = false;
        _inputs = null;
    }

    private bool IsAttackHeld()
    {
        return _inputs != null && _inputs.Player.Attack.IsPressed();
    }

    private void HandleAttackReleased(InputAction.CallbackContext context)
    {
        if (_sessionActive)
            CancelActiveSession();
    }

    private void HandleSkillCheck(InputAction.CallbackContext context)
    {
        if (!_sessionActive || !_skillCheckActive || !IsAttackHeld())
            return;

        uint challengeId = _skillCheckId;
        ClearSkillCheck();
        _tamingController?.RequestSkillCheck(_sessionId, challengeId);
    }

    private void SendHeartbeat()
    {
        _tamingController?.RequestHoldPulse(_sessionId);
        _nextHeartbeatTime = Time.unscaledTime + _heartbeatInterval;
    }

    private void CancelActiveSession()
    {
        ulong sessionId = _sessionId;
        ClearSession();
        _tamingController?.RequestCancelOverride(sessionId);
    }

    private void ClearSession()
    {
        _sessionActive = false;
        _sessionId = 0;
        _nextHeartbeatTime = 0f;
        ClearSkillCheck();
    }

    private void ClearSkillCheck()
    {
        _skillCheckActive = false;
        _skillCheckId = 0;
    }

    private void OnDestroy()
    {
        InputBootstrap.OnLocalReady -= HandleInputReady;
        UnbindInput();
        ClearSession();
    }
}
