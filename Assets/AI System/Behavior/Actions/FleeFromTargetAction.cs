using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Artifika.AI;
using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flee From Target", story: "[Blackboard] flees from [Target]", category: "Action", id: "6c6f1c56738af7ae4cc3699784c17004")]
public partial class FleeFromTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;
    [SerializeReference] public BlackboardVariable<GameEntity> Target;

    private const float MinFleeDistance = 30f;
    private const float FleeDistanceIntervalMultiplier = 10f;
    private const float FleeSpeedLookaheadSeconds = 3f;
    private const float RetryCooldown = 0.75f;
    private const float SafetyReevaluationInterval = 1.5f;
    private const float DirectionDriftThreshold = 60f;

    private MovementModule _movementModule;
    private Blackboard _blackboard;
    private Transform _threat;

    private Vector3 _fleeDestination;
    private Vector3 _lastFleeDirection;
    private float _preferredSide;
    private float _nextRetryTime;
    private float _nextSafetyCheckTime;
    private bool _hasDestination;
    private bool _restoreAutoBraking;

    protected override Status OnStart()
    {
        _blackboard = Blackboard?.Value;
        _movementModule = _blackboard?.movementModule;
        _threat = Target?.Value ? Target.Value.transform : null;

        if (_movementModule == null || _threat == null)
            return Status.Failure;

        _preferredSide = 0f;
        _hasDestination = false;
        _restoreAutoBraking = _movementModule.AutoBraking;
        _movementModule.SetAutoBraking(false);

        PickNextFleeDestination();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_movementModule == null)
            return Status.Failure;

        if (_threat == null)
            return Status.Success;

        if (NeedsNewDestination())
            PickNextFleeDestination();

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_movementModule == null)
            return;

        _movementModule.SetAutoBraking(_restoreAutoBraking);

        if (!IsStillFleeing())
            _movementModule.Stop();
    }

    private bool NeedsNewDestination()
    {
        if (!_hasDestination)
            return Time.time >= _nextRetryTime;

        if (_movementModule.HasArrived())
            return true;

        if (_movementModule.HasBlockedPath)
            return true;

        if (_movementModule.IsStuck())
            return true;

        return Time.time >= _nextSafetyCheckTime && HasFleeDirectionDrifted();
    }

    private bool HasFleeDirectionDrifted()
    {
        Vector3 currentAwayDir = _movementModule.transform.position - _threat.position;
        currentAwayDir.y = 0f;

        if (currentAwayDir.sqrMagnitude < 0.01f)
            return false;

        return Vector3.Angle(_lastFleeDirection, currentAwayDir.normalized) > DirectionDriftThreshold;
    }

    private void PickNextFleeDestination()
    {
        MovementProfile profile = _movementModule.MovementProfile;
        if (profile == null)
        {
            FailPick();
            return;
        }

        Vector3 agentPos = _movementModule.transform.position;
        Vector3 threatPos = _threat.position;
        float fleeSpeed = CalculateFleeSpeed(profile);
        float fleeDistance = CalculateFleeDistance(profile, fleeSpeed);

        Vector3 awayDir = agentPos - threatPos;
        awayDir.y = 0f;
        _lastFleeDirection = awayDir.sqrMagnitude > 0.01f ? awayDir.normalized : Vector3.forward;

        if (!MovementPointPicker.TryPickFleePoint(agentPos, threatPos, fleeDistance, _preferredSide, out _fleeDestination, out float chosenSide))
        {
            FailPick();
            return;
        }

        _preferredSide = chosenSide;
        _movementModule.SetSpeed(fleeSpeed);

        if (_movementModule.MoveTo(_fleeDestination))
        {
            _hasDestination = true;
            _nextSafetyCheckTime = Time.time + SafetyReevaluationInterval;
        }
        else
        {
            FailPick();
        }
    }

    private void FailPick()
    {
        _movementModule.Stop();
        _hasDestination = false;
        _nextRetryTime = Time.time + RetryCooldown;
    }

    private static float CalculateFleeSpeed(MovementProfile profile)
    {
        return profile.runMovementSpeed > 0f
            ? profile.runMovementSpeed
            : profile.walkMovementSpeed;
    }

    private static float CalculateFleeDistance(MovementProfile profile, float fleeSpeed)
    {
        float intervalDistance = profile.runDistanceInterval * FleeDistanceIntervalMultiplier;
        float speedLookaheadDistance = fleeSpeed * FleeSpeedLookaheadSeconds;
        return Mathf.Max(intervalDistance, speedLookaheadDistance, MinFleeDistance);
    }

    private bool IsStillFleeing()
    {
        return _blackboard != null
               && _blackboard.aggroModule != null
               && _blackboard.aggroModule.CurrentAggroState == AggroState.Alerted
               && Target?.Value != null;
    }
}