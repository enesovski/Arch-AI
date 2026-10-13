using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct OverrideSkillCheckSettings
{
    [Min(1)] public int requiredSuccessfulChecks;
    [Tooltip("Seconds for one full needle revolution. Lower values produce a faster needle.")]
    [Min(0.25f)] public float indicatorDurationSeconds;
    [Range(0.05f, 0.9f)] public float successZoneNormalizedSize;
    [Min(0f)] public float firstCheckDelaySeconds;
    [Min(0f)] public float delayBetweenChecksSeconds;
    [Tooltip("Stun seconds removed by each unsuccessful skill check.")]
    [Min(0.05f)] public float failedCheckStunTimePenaltySeconds;
    [Tooltip("Immediately reverse from the pressed position and start the next check without pausing the needle.")]
    public bool continueNeedleImmediatelyAfterCheck;

    public static OverrideSkillCheckSettings PrototypeDefault => new()
    {
        requiredSuccessfulChecks = 3,
        indicatorDurationSeconds = 2f,
        successZoneNormalizedSize = 0.25f,
        firstCheckDelaySeconds = 0.5f,
        delayBetweenChecksSeconds = 0.4f,
        failedCheckStunTimePenaltySeconds = 1.5f,
        continueNeedleImmediatelyAfterCheck = false,
    };

    public readonly OverrideSkillCheckSettings Sanitized()
    {
        OverrideSkillCheckSettings defaults = PrototypeDefault;

        return new OverrideSkillCheckSettings
        {
            requiredSuccessfulChecks = requiredSuccessfulChecks > 0
                ? requiredSuccessfulChecks
                : defaults.requiredSuccessfulChecks,
            indicatorDurationSeconds = indicatorDurationSeconds > 0f
                ? Mathf.Max(0.25f, indicatorDurationSeconds)
                : defaults.indicatorDurationSeconds,
            successZoneNormalizedSize = successZoneNormalizedSize > 0f
                ? Mathf.Clamp(successZoneNormalizedSize, 0.05f, 0.9f)
                : defaults.successZoneNormalizedSize,
            firstCheckDelaySeconds = Mathf.Max(0f, firstCheckDelaySeconds),
            delayBetweenChecksSeconds = Mathf.Max(0f, delayBetweenChecksSeconds),
            failedCheckStunTimePenaltySeconds = failedCheckStunTimePenaltySeconds > 0f
                ? Mathf.Max(0.05f, failedCheckStunTimePenaltySeconds)
                : defaults.failedCheckStunTimePenaltySeconds,
            continueNeedleImmediatelyAfterCheck = continueNeedleImmediatelyAfterCheck,
        };
    }
}

public enum OverrideSkillCheckOutcome : byte
{
    Success,
    Failed,
    Expired,
}

/// <summary>
/// Client-side session data for the future override HUD.
/// </summary>
public readonly struct LocalOverrideSessionData
{
    public readonly ulong SessionId;
    public readonly NetworkObject Target;
    public readonly int RequiredSuccessfulChecks;
    public readonly double StunEndsAtServerTime;
    public readonly float StunDurationSeconds;

    public LocalOverrideSessionData(
        ulong sessionId,
        NetworkObject target,
        int requiredSuccessfulChecks,
        double stunEndsAtServerTime,
        float stunDurationSeconds)
    {
        SessionId = sessionId;
        Target = target;
        RequiredSuccessfulChecks = requiredSuccessfulChecks;
        StunEndsAtServerTime = stunEndsAtServerTime;
        StunDurationSeconds = stunDurationSeconds;
    }
}

/// <summary>
/// A server-authored challenge. ServerTime makes the later UI deterministic on host and clients.
/// </summary>
public readonly struct LocalOverrideSkillCheckData
{
    public readonly ulong SessionId;
    public readonly uint ChallengeId;
    public readonly double StartsAtServerTime;
    public readonly float DurationSeconds;
    public readonly float SuccessZoneStart;
    public readonly float SuccessZoneSize;
    public readonly float NeedleStartPosition;
    public readonly bool Clockwise;

    public LocalOverrideSkillCheckData(
        ulong sessionId,
        uint challengeId,
        double startsAtServerTime,
        float durationSeconds,
        float successZoneStart,
        float successZoneSize)
        : this(
            sessionId,
            challengeId,
            startsAtServerTime,
            durationSeconds,
            successZoneStart,
            successZoneSize,
            0f,
            true)
    {
    }

    public LocalOverrideSkillCheckData(
        ulong sessionId,
        uint challengeId,
        double startsAtServerTime,
        float durationSeconds,
        float successZoneStart,
        float successZoneSize,
        float needleStartPosition,
        bool clockwise)
    {
        SessionId = sessionId;
        ChallengeId = challengeId;
        StartsAtServerTime = startsAtServerTime;
        DurationSeconds = durationSeconds;
        SuccessZoneStart = successZoneStart;
        SuccessZoneSize = successZoneSize;
        NeedleStartPosition = Mathf.Repeat(needleStartPosition, 1f);
        Clockwise = clockwise;
    }

    public float GetNormalizedTravelProgress(double serverTime)
    {
        if (DurationSeconds <= 0f)
            return 0f;

        float elapsedRevolutions = Mathf.Max(
            0f,
            (float)((serverTime - StartsAtServerTime) / DurationSeconds));
        return Mathf.Repeat(elapsedRevolutions, 1f);
    }

    public float GetNormalizedIndicatorPosition(double serverTime)
    {
        float travel = GetNormalizedTravelProgress(serverTime);
        float signedTravel = Clockwise ? travel : -travel;
        return Mathf.Repeat(NeedleStartPosition + signedTravel, 1f);
    }

    public bool Contains(float normalizedPosition)
    {
        return normalizedPosition >= SuccessZoneStart &&
               normalizedPosition <= SuccessZoneStart + SuccessZoneSize;
    }
}

public readonly struct LocalOverrideSkillCheckResult
{
    public readonly ulong SessionId;
    public readonly uint ChallengeId;
    public readonly OverrideSkillCheckOutcome Outcome;
    public readonly float EvaluatedIndicatorPosition;
    public readonly int CompletedSuccessfulChecks;
    public readonly int RequiredSuccessfulChecks;
    public readonly double StunEndsAtServerTime;
    public readonly float StunTimePenaltySeconds;

    public LocalOverrideSkillCheckResult(
        ulong sessionId,
        uint challengeId,
        OverrideSkillCheckOutcome outcome,
        float evaluatedIndicatorPosition,
        int completedSuccessfulChecks,
        int requiredSuccessfulChecks,
        double stunEndsAtServerTime = 0d,
        float stunTimePenaltySeconds = 0f)
    {
        SessionId = sessionId;
        ChallengeId = challengeId;
        Outcome = outcome;
        EvaluatedIndicatorPosition = evaluatedIndicatorPosition;
        CompletedSuccessfulChecks = completedSuccessfulChecks;
        RequiredSuccessfulChecks = requiredSuccessfulChecks;
        StunEndsAtServerTime = stunEndsAtServerTime;
        StunTimePenaltySeconds = stunTimePenaltySeconds;
    }
}
