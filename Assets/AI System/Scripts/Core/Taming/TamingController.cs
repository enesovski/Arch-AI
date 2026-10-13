using System;
using System.Collections.Generic;
using Artifika.AI;
using Artifika.Downed;
using Artifika.States;
using NewInventorySystem;
using Unity.Netcode;
using UnityEngine;

public enum OverrideEndReason : byte
{
    None,
    Released,
    HeartbeatTimeout,
    TargetNoLongerTameable,
    OutOfRange,
    OverrideToolLost,
    PlayerUnavailable,
    TargetUnavailable,
    PlayerStateChanged,
    Disconnected,
    Completed,
    PlayerDowned,
}

[DisallowMultipleComponent]
public class TamingController : NetworkBehaviour, IDownedInterruptible
{
    public static TamingController Instance { get; private set; }
    public double LocalServerTime => NetworkManager != null
        ? NetworkManager.ServerTime.Time
        : Time.realtimeSinceStartupAsDouble;

    public event Action<LocalOverrideSessionData> LocalOverrideStarted;
    public event Action<LocalOverrideSkillCheckData> LocalSkillCheckStarted;
    public event Action<LocalOverrideSkillCheckResult> LocalSkillCheckResolved;
    public event Action<ulong, OverrideEndReason> LocalOverrideEnded;

    [Header("Override Prototype")]
    [SerializeField, Min(0.1f)] private float maxOverrideDistance = 4f;
    [SerializeField, Min(0.05f)] private float holdPulseInterval = 0.25f;
    [SerializeField, Min(0.2f)] private float holdPulseTimeout = 0.75f;
    [SerializeField, Min(0.02f)] private float sessionAuditInterval = 0.1f;
    [SerializeField, Min(0.05f)] private float skillCheckLeadTime = 0.35f;
    [SerializeField, Min(0f)] private float maxLatencyCompensation = 0.15f;
    [SerializeField] private string overridingStateId = "Overriding";
    [SerializeField] private bool logPrototypeFlow = true;

    [Header("Quest Progress")]
    [SerializeField] private DynamicWorldFactKey tamedBotFactKey;

    private readonly Dictionary<ulong, OverrideSession> _sessionsByClient = new();
    private readonly Dictionary<ulong, ulong> _targetClaims = new();
    private readonly List<PendingCancellation> _pendingCancellations = new();
    private readonly System.Random _skillCheckRandom = new();

    private OverrideInputController _localInput;
    private ulong _nextSessionId = 1;
    private double _nextAuditTime;

    private sealed class OverrideSession
    {
        public ulong SessionId;
        public ulong ClientId;
        public ulong TargetNetworkObjectId;
        public NetworkObject PlayerObject;
        public NetworkObject TargetObject;
        public Blackboard TargetBlackboard;
        public Collider[] TargetHitboxes;
        public PlayerStateMachine PlayerStateMachine;
        public string ReturnStateId;
        public OverrideSkillCheckSettings SkillCheckSettings;
        public int RequiredSuccesses;
        public int CompletedSuccesses;
        public double LastHoldPulseTime;
        public double NextChallengeIssueTime;
        public uint NextChallengeId;
        public bool ChallengeActive;
        public uint ActiveChallengeId;
        public double ChallengeStartsAt;
        public float SuccessZoneStart;
        public float SuccessZoneSize;
        public float NeedleStartPosition;
        public bool NeedleClockwise;
        public double StunEndsAtServerTime;
        public float StunDurationSeconds;
    }

    private readonly struct PendingCancellation
    {
        public readonly ulong ClientId;
        public readonly OverrideEndReason Reason;

        public PendingCancellation(ulong clientId, OverrideEndReason reason)
        {
            ClientId = clientId;
            Reason = reason;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            GameLog.Warning("[TamingController] Multiple instances found. Keeping the first instance.");
            return;
        }

        Instance = this;

        _localInput = GetComponent<OverrideInputController>();
        if (_localInput == null)
            _localInput = gameObject.AddComponent<OverrideInputController>();

        _localInput.Initialize(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            DownedInterruptRegistry.Register(this);
        }
    }

    /// <summary>
    /// A downed player cannot continue an override. Ending the session first returns
    /// them to their stored return state, which DownedPlayerService then overwrites
    /// with Injured — so this must run before the forced state change, as it does.
    /// </summary>
    public void ServerInterruptForDowned(ulong clientId, NetworkObject playerNetObj)
    {
        if (!IsServer) return;
        CancelSession(clientId, OverrideEndReason.PlayerDowned);
    }

    private void Update()
    {
        if (!IsServer || _sessionsByClient.Count == 0)
            return;

        double now = LocalServerTime;
        if (now < _nextAuditTime)
            return;

        _nextAuditTime = now + sessionAuditInterval;
        _pendingCancellations.Clear();

        foreach (KeyValuePair<ulong, OverrideSession> pair in _sessionsByClient)
        {
            OverrideEndReason reason = GetInvalidSessionReason(pair.Value, now);
            if (reason != OverrideEndReason.None)
            {
                _pendingCancellations.Add(new PendingCancellation(pair.Key, reason));
                continue;
            }

            UpdateSkillCheckSession(pair.Value, now);
        }

        foreach (PendingCancellation cancellation in _pendingCancellations)
            CancelSession(cancellation.ClientId, cancellation.Reason);
    }

    /// <summary>
    /// Called directly on the server by the authenticated player AttackHandler RPC
    /// after its spherecast contacts a stunned tameable target.
    /// </summary>
    public bool TryStartOverrideServer(ulong clientId, NetworkObject playerObject, GameEntity target)
    {
        if (!IsServer)
            return false;

        if (!TryValidateStart(clientId, playerObject, target, out NetworkObject targetObject,
                out Blackboard targetBlackboard, out Collider[] targetHitboxes,
                out PlayerStateMachine playerStateMachine, out string rejection))
        {
            Log($"Start rejected for client {clientId}: {rejection}");
            return false;
        }

        OverrideSkillCheckSettings settings = target.entityConfiguration != null
            ? target.entityConfiguration.overrideSkillChecks.Sanitized()
            : OverrideSkillCheckSettings.PrototypeDefault;
        double now = LocalServerTime;
        float stunRemainingSeconds = 0f;
        float stunDurationSeconds = 0f;
        targetBlackboard.entityStatsController?.TryGetStunTiming(
            out stunRemainingSeconds,
            out stunDurationSeconds);
        double stunEndsAtServerTime = now + Mathf.Max(0f, stunRemainingSeconds);

        var session = new OverrideSession
        {
            SessionId = _nextSessionId++,
            ClientId = clientId,
            TargetNetworkObjectId = targetObject.NetworkObjectId,
            PlayerObject = playerObject,
            TargetObject = targetObject,
            TargetBlackboard = targetBlackboard,
            TargetHitboxes = targetHitboxes,
            PlayerStateMachine = playerStateMachine,
            ReturnStateId = playerStateMachine.CurrentId,
            SkillCheckSettings = settings,
            RequiredSuccesses = settings.requiredSuccessfulChecks,
            CompletedSuccesses = 0,
            LastHoldPulseTime = now,
            NextChallengeIssueTime = now + settings.firstCheckDelaySeconds,
            NextChallengeId = 1,
            NeedleStartPosition = 0f,
            NeedleClockwise = true,
            StunEndsAtServerTime = stunEndsAtServerTime,
            StunDurationSeconds = stunDurationSeconds,
        };

        _sessionsByClient.Add(clientId, session);
        _targetClaims.Add(session.TargetNetworkObjectId, clientId);

        playerStateMachine.ForceChangeStateServer(overridingStateId);

        BeginOverrideClientRpc(
            session.SessionId,
            targetObject,
            session.RequiredSuccesses,
            GetSafeHoldPulseInterval(),
            stunEndsAtServerTime,
            stunDurationSeconds,
            GetTargetClientRpcParams(clientId));

        Log($"Session {session.SessionId} started. Client:{clientId} Target:{target.name} Checks:{session.RequiredSuccesses}");
        return true;
    }

    public void RequestHoldPulse(ulong sessionId)
    {
        if (IsSpawned)
            HoldPulseServerRpc(sessionId);
    }

    public void RequestCancelOverride(ulong sessionId)
    {
        if (IsSpawned)
            CancelOverrideServerRpc(sessionId);
    }

    public void RequestSkillCheck(ulong sessionId, uint challengeId)
    {
        if (IsSpawned)
            SubmitSkillCheckServerRpc(sessionId, challengeId);
    }

    public void DebugStun(GameEntity target)
    {
        NetworkObject targetNetObj = GetNetworkObject(target);
        if (targetNetObj == null)
            return;

        DebugStunServerRpc(targetNetObj);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HoldPulseServerRpc(ulong sessionId, ServerRpcParams rpcParams = default)
    {
        if (!TryGetSenderSession(rpcParams.Receive.SenderClientId, sessionId, out OverrideSession session))
            return;

        session.LastHoldPulseTime = LocalServerTime;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CancelOverrideServerRpc(ulong sessionId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!TryGetSenderSession(clientId, sessionId, out _))
            return;

        CancelSession(clientId, OverrideEndReason.Released);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSkillCheckServerRpc(
        ulong sessionId,
        uint challengeId,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!TryGetSenderSession(clientId, sessionId, out OverrideSession session))
            return;

        double now = LocalServerTime;
        OverrideEndReason invalidReason = GetInvalidSessionReason(session, now);
        if (invalidReason != OverrideEndReason.None)
        {
            CancelSession(clientId, invalidReason);
            return;
        }

        if (!session.ChallengeActive || session.ActiveChallengeId != challengeId)
            return;

        double evaluatedTime = now - GetLatencyCompensationSeconds(clientId);
        float normalizedPosition = GetNormalizedIndicatorPosition(session, evaluatedTime);
        bool challengeStarted = evaluatedTime >= session.ChallengeStartsAt;
        bool success = challengeStarted && IsInsideSuccessZone(session, normalizedPosition);

        ResolveSkillCheck(
            session,
            success ? OverrideSkillCheckOutcome.Success : OverrideSkillCheckOutcome.Failed,
            normalizedPosition,
            now);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugStunServerRpc(NetworkObjectReference targetRef)
    {
        if (!targetRef.TryGet(out NetworkObject targetNetObj))
            return;

        EntityStatsController stats = targetNetObj.GetComponentInChildren<EntityStatsController>(true);
        stats?.ApplyStanceDamage(float.MaxValue);
    }

    [ClientRpc]
    private void BeginOverrideClientRpc(
        ulong sessionId,
        NetworkObjectReference targetRef,
        int requiredSuccesses,
        float heartbeatInterval,
        double stunEndsAtServerTime,
        float stunDurationSeconds,
        ClientRpcParams rpcParams = default)
    {
        _localInput?.BeginSession(sessionId, heartbeatInterval);
        targetRef.TryGet(out NetworkObject targetObject);
        LocalOverrideStarted?.Invoke(new LocalOverrideSessionData(
            sessionId,
            targetObject,
            requiredSuccesses,
            stunEndsAtServerTime,
            stunDurationSeconds));
        Log($"Session {sessionId} accepted. Keep holding Attack and complete SkillCheck inputs (0/{requiredSuccesses}).");
    }

    [ClientRpc]
    private void StartSkillCheckClientRpc(
        ulong sessionId,
        uint challengeId,
        double startsAtServerTime,
        float durationSeconds,
        float successZoneStart,
        float successZoneSize,
        float needleStartPosition,
        bool clockwise,
        ClientRpcParams rpcParams = default)
    {
        _localInput?.BeginSkillCheck(sessionId, challengeId);
        LocalSkillCheckStarted?.Invoke(new LocalOverrideSkillCheckData(
            sessionId,
            challengeId,
            startsAtServerTime,
            durationSeconds,
            successZoneStart,
            successZoneSize,
            needleStartPosition,
            clockwise));
    }

    [ClientRpc]
    private void ResolveSkillCheckClientRpc(
        ulong sessionId,
        uint challengeId,
        OverrideSkillCheckOutcome outcome,
        float evaluatedIndicatorPosition,
        int completedSuccesses,
        int requiredSuccesses,
        double stunEndsAtServerTime,
        float stunTimePenaltySeconds,
        ClientRpcParams rpcParams = default)
    {
        _localInput?.EndSkillCheck(sessionId, challengeId);
        LocalSkillCheckResolved?.Invoke(new LocalOverrideSkillCheckResult(
            sessionId,
            challengeId,
            outcome,
            evaluatedIndicatorPosition,
            completedSuccesses,
            requiredSuccesses,
            stunEndsAtServerTime,
            stunTimePenaltySeconds));
    }

    [ClientRpc]
    private void EndOverrideClientRpc(
        ulong sessionId,
        OverrideEndReason reason,
        ClientRpcParams rpcParams = default)
    {
        _localInput?.EndSession(sessionId);
        LocalOverrideEnded?.Invoke(sessionId, reason);
        Log($"Session {sessionId} ended: {reason}");
    }

    private bool TryValidateStart(
        ulong clientId,
        NetworkObject playerObject,
        GameEntity requestedTarget,
        out NetworkObject targetObject,
        out Blackboard targetBlackboard,
        out Collider[] targetHitboxes,
        out PlayerStateMachine playerStateMachine,
        out string rejection)
    {
        targetObject = null;
        targetBlackboard = null;
        targetHitboxes = Array.Empty<Collider>();
        playerStateMachine = null;
        rejection = null;

        if (playerObject == null || !playerObject.IsSpawned || playerObject.OwnerClientId != clientId)
        {
            rejection = "player object is invalid or not owned by the sender";
            return false;
        }

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
            client.PlayerObject == null ||
            client.PlayerObject.NetworkObjectId != playerObject.NetworkObjectId)
        {
            rejection = "sender is not the connected player object";
            return false;
        }

        if (_sessionsByClient.ContainsKey(clientId))
        {
            rejection = "player already has an active override session";
            return false;
        }

        targetObject = GetNetworkObject(requestedTarget);
        if (targetObject == null || !targetObject.IsSpawned)
        {
            rejection = "target NetworkObject is unavailable";
            return false;
        }

        if (_targetClaims.ContainsKey(targetObject.NetworkObjectId))
        {
            rejection = "target is already claimed";
            return false;
        }

        targetBlackboard = GetBlackboard(targetObject);
        if (targetBlackboard == null || !targetBlackboard.CanBeTamed())
        {
            rejection = "target is not currently stunned and tameable";
            return false;
        }

        targetHitboxes = GetOverrideTargetHitboxes(targetBlackboard);
        if (!IsWithinOverrideDistance(playerObject, targetObject, targetHitboxes))
        {
            rejection = "target is outside override distance";
            return false;
        }

        if (!HasOverrideToolEquipped(playerObject))
        {
            rejection = "override tool is not equipped";
            return false;
        }

        playerStateMachine = playerObject.GetComponentInChildren<PlayerStateMachine>(true);
        if (playerStateMachine == null)
        {
            rejection = "PlayerStateMachine is missing";
            return false;
        }

        if (!playerStateMachine.HasPermission(PlayerPermission.Attack))
        {
            rejection = "current player state does not allow attacks";
            return false;
        }

        if (!playerStateMachine.HasStateProfile(overridingStateId))
        {
            rejection = $"player state catalog is missing '{overridingStateId}'";
            return false;
        }

        return true;
    }

    private OverrideEndReason GetInvalidSessionReason(OverrideSession session, double now)
    {
        if (session.PlayerObject == null || !session.PlayerObject.IsSpawned)
            return OverrideEndReason.PlayerUnavailable;

        if (session.TargetObject == null || !session.TargetObject.IsSpawned)
            return OverrideEndReason.TargetUnavailable;

        if (session.PlayerStateMachine == null || session.PlayerStateMachine.CurrentId != overridingStateId)
            return OverrideEndReason.PlayerStateChanged;

        if (session.TargetBlackboard == null || !session.TargetBlackboard.CanBeTamed())
            return OverrideEndReason.TargetNoLongerTameable;

        if (!IsWithinOverrideDistance(session.PlayerObject, session.TargetObject, session.TargetHitboxes))
            return OverrideEndReason.OutOfRange;

        if (!HasOverrideToolEquipped(session.PlayerObject))
            return OverrideEndReason.OverrideToolLost;

        if (now - session.LastHoldPulseTime > holdPulseTimeout)
            return OverrideEndReason.HeartbeatTimeout;

        return OverrideEndReason.None;
    }

    private void UpdateSkillCheckSession(OverrideSession session, double now)
    {
        if (session.ChallengeActive)
            return;

        if (now >= session.NextChallengeIssueTime)
            IssueSkillCheck(session, now);
    }

    private void IssueSkillCheck(OverrideSession session, double now, bool startImmediately = false)
    {
        OverrideSkillCheckSettings settings = session.SkillCheckSettings;
        float zoneSize = settings.successZoneNormalizedSize;
        const float edgePadding = 0.05f;
        float minimumStart = edgePadding;
        float maximumStart = 1f - zoneSize - edgePadding;

        if (maximumStart < minimumStart)
        {
            minimumStart = 0f;
            maximumStart = 1f - zoneSize;
        }

        float zoneStart = Mathf.Lerp(
            minimumStart,
            maximumStart,
            (float)_skillCheckRandom.NextDouble());

        uint challengeId = session.NextChallengeId++;
        if (session.NextChallengeId == 0)
            session.NextChallengeId = 1;

        session.ChallengeActive = true;
        session.ActiveChallengeId = challengeId;
        session.ChallengeStartsAt = startImmediately
            ? now
            : now + Mathf.Max(0.05f, skillCheckLeadTime);
        session.SuccessZoneStart = zoneStart;
        session.SuccessZoneSize = zoneSize;

        StartSkillCheckClientRpc(
            session.SessionId,
            challengeId,
            session.ChallengeStartsAt,
            settings.indicatorDurationSeconds,
            zoneStart,
            zoneSize,
            session.NeedleStartPosition,
            session.NeedleClockwise,
            GetTargetClientRpcParams(session.ClientId));

        Log(
            $"Session {session.SessionId} challenge {challengeId} issued " +
            $"at {session.NeedleStartPosition:F3} " +
            $"({(session.NeedleClockwise ? "CW" : "CCW")}).");
    }

    private void ResolveSkillCheck(
        OverrideSession session,
        OverrideSkillCheckOutcome outcome,
        float normalizedPosition,
        double now)
    {
        if (!session.ChallengeActive)
            return;

        uint challengeId = session.ActiveChallengeId;
        session.ChallengeActive = false;
        session.ActiveChallengeId = 0;
        session.NeedleStartPosition = Mathf.Repeat(normalizedPosition, 1f);
        if (outcome != OverrideSkillCheckOutcome.Expired)
            session.NeedleClockwise = !session.NeedleClockwise;

        if (outcome == OverrideSkillCheckOutcome.Success)
            session.CompletedSuccesses++;

        float stunTimePenaltySeconds = 0f;
        if (outcome != OverrideSkillCheckOutcome.Success)
        {
            EntityStatsController stats = session.TargetBlackboard != null
                ? session.TargetBlackboard.entityStatsController
                : null;
            if (stats != null && stats.TryReduceStunTime(
                    session.SkillCheckSettings.failedCheckStunTimePenaltySeconds,
                    out float remainingStunSeconds,
                    out float stunDurationSeconds,
                    out stunTimePenaltySeconds))
            {
                session.StunEndsAtServerTime = now + remainingStunSeconds;
                session.StunDurationSeconds = stunDurationSeconds;
            }
        }

        ResolveSkillCheckClientRpc(
            session.SessionId,
            challengeId,
            outcome,
            normalizedPosition,
            session.CompletedSuccesses,
            session.RequiredSuccesses,
            session.StunEndsAtServerTime,
            stunTimePenaltySeconds,
            GetTargetClientRpcParams(session.ClientId));

        Log($"Session {session.SessionId} challenge {challengeId}: {outcome} at {normalizedPosition:F3}. Progress:{session.CompletedSuccesses}/{session.RequiredSuccesses}");

        if (session.CompletedSuccesses >= session.RequiredSuccesses)
        {
            CompleteSession(session);
            return;
        }

        if (session.TargetBlackboard == null || !session.TargetBlackboard.CanBeTamed())
        {
            CancelSession(session.ClientId, OverrideEndReason.TargetNoLongerTameable);
            return;
        }

        if (session.SkillCheckSettings.continueNeedleImmediatelyAfterCheck)
        {
            IssueSkillCheck(session, now, true);
            return;
        }

        session.NextChallengeIssueTime = now + session.SkillCheckSettings.delayBetweenChecksSeconds;
    }

    private static float GetNormalizedIndicatorPosition(OverrideSession session, double serverTime)
    {
        double duration = session.SkillCheckSettings.indicatorDurationSeconds;
        if (duration <= 0d)
            return 0f;

        float elapsedRevolutions = Mathf.Max(
            0f,
            (float)((serverTime - session.ChallengeStartsAt) / duration));
        float travel = Mathf.Repeat(elapsedRevolutions, 1f);
        float signedTravel = session.NeedleClockwise ? travel : -travel;
        return Mathf.Repeat(session.NeedleStartPosition + signedTravel, 1f);
    }

    private static bool IsInsideSuccessZone(OverrideSession session, float normalizedPosition)
    {
        return normalizedPosition >= session.SuccessZoneStart &&
               normalizedPosition <= session.SuccessZoneStart + session.SuccessZoneSize;
    }

    private double GetLatencyCompensationSeconds(ulong clientId)
    {
        if (NetworkManager == null || clientId == NetworkManager.ServerClientId ||
            NetworkManager.NetworkConfig?.NetworkTransport == null)
        {
            return 0d;
        }

        ulong roundTripMilliseconds = NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId);
        double estimatedOneWaySeconds = roundTripMilliseconds * 0.0005d;
        return Math.Min(Mathf.Max(0f, maxLatencyCompensation), estimatedOneWaySeconds);
    }

    private bool TryGetSenderSession(ulong clientId, ulong sessionId, out OverrideSession session)
    {
        return _sessionsByClient.TryGetValue(clientId, out session) && session.SessionId == sessionId;
    }

    private void CompleteSession(OverrideSession session)
    {
        GameEntity player = GetGameEntity(session.PlayerObject);
        GameEntity target = session.TargetBlackboard != null
            ? session.TargetBlackboard.gameEntity
            : GetGameEntity(session.TargetObject);

        EndSession(session, OverrideEndReason.Completed, notifyClient: true);

        if (player == null || target == null)
        {
            GameLog.Warning("[TamingController] Completed override could not resolve player or target.");
            return;
        }

        SpawnTamed(player, target);
    }

    private void CancelSession(ulong clientId, OverrideEndReason reason)
    {
        if (!_sessionsByClient.TryGetValue(clientId, out OverrideSession session))
            return;

        EndSession(session, reason, notifyClient: reason != OverrideEndReason.Disconnected);
    }

    private void EndSession(OverrideSession session, OverrideEndReason reason, bool notifyClient)
    {
        _sessionsByClient.Remove(session.ClientId);
        _targetClaims.Remove(session.TargetNetworkObjectId);

        if (session.PlayerStateMachine != null && session.PlayerStateMachine.IsSpawned &&
            session.PlayerStateMachine.CurrentId == overridingStateId)
        {
            string returnState = session.PlayerStateMachine.HasStateProfile(session.ReturnStateId)
                ? session.ReturnStateId
                : PlayerStateId.GAMEPLAY;

            session.PlayerStateMachine.ForceChangeStateServer(returnState);
        }

        if (notifyClient && NetworkManager.ConnectedClients.ContainsKey(session.ClientId))
        {
            EndOverrideClientRpc(
                session.SessionId,
                reason,
                GetTargetClientRpcParams(session.ClientId));
        }

        Log($"Session {session.SessionId} cleaned up. Reason:{reason}. Progress discarded:{session.CompletedSuccesses}/{session.RequiredSuccesses}");
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        CancelSession(clientId, OverrideEndReason.Disconnected);
    }

    private static Collider[] GetOverrideTargetHitboxes(Blackboard targetBlackboard)
    {
        RegionHitbox[] regionHitboxes = targetBlackboard.GetComponentsInChildren<RegionHitbox>(true);
        if (regionHitboxes.Length == 0)
            return Array.Empty<Collider>();

        var colliders = new List<Collider>(regionHitboxes.Length);
        foreach (RegionHitbox regionHitbox in regionHitboxes)
        {
            Collider collider = regionHitbox.GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
                colliders.Add(collider);
        }

        return colliders.ToArray();
    }

    private bool IsWithinOverrideDistance(
        NetworkObject playerObject,
        NetworkObject targetObject,
        Collider[] targetHitboxes)
    {
        float maxDistance = Mathf.Max(0.1f, maxOverrideDistance);
        float maxDistanceSquared = maxDistance * maxDistance;
        Vector3 playerPosition = playerObject.transform.position;
        bool hasActiveHitbox = false;

        if (targetHitboxes != null)
        {
            foreach (Collider targetHitbox in targetHitboxes)
            {
                if (targetHitbox == null || !targetHitbox.enabled || !targetHitbox.gameObject.activeInHierarchy)
                    continue;

                hasActiveHitbox = true;
                Vector3 closestPoint = targetHitbox.ClosestPoint(playerPosition);
                if ((playerPosition - closestPoint).sqrMagnitude <= maxDistanceSquared)
                    return true;
            }
        }

        if (hasActiveHitbox)
            return false;

        return (playerPosition - targetObject.transform.position).sqrMagnitude <= maxDistanceSquared;
    }

    private static bool HasOverrideToolEquipped(NetworkObject playerObject)
    {
        WeaponManager weaponManager = playerObject.GetComponentInChildren<WeaponManager>(true);
        if (weaponManager == null || weaponManager.CurrentEquippedItemData.IsEmpty || ItemDatabase.Instance == null)
            return false;

        ItemData itemData = weaponManager.CurrentEquippedItemData;
        ItemSO item = ItemDatabase.Instance.GetItemByID(itemData.ItemId);
        return item != null && item.CanOverride;
    }

    private float GetSafeHoldPulseInterval()
    {
        return Mathf.Clamp(holdPulseInterval, 0.05f, Mathf.Max(0.05f, holdPulseTimeout * 0.5f));
    }

    private static ClientRpcParams GetTargetClientRpcParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
    }

    private void SpawnTamed(GameEntity player, GameEntity target)
    {
        if (!IsServer) return;

        GameObject prefab = target.entityConfiguration != null ? target.entityConfiguration.tamedPrefab : null;
        NetworkObject playerNetObj = GetNetworkObject(player);
        NetworkObject targetNetObj = GetNetworkObject(target);

        if (prefab == null || playerNetObj == null || targetNetObj == null)
        {
            GameLog.Warning($"[TamingController] Cannot spawn tamed entity. Prefab:{prefab != null} PlayerNetObj:{playerNetObj != null} TargetNetObj:{targetNetObj != null}");
            return;
        }

        Vector3 position = target.transform.position;
        Quaternion rotation = target.transform.rotation;
        string name = target.entityConfiguration != null ? target.entityConfiguration.entityName : target.name;

        if (TamedEntitySpawner.Instance == null)
        {
            GameLog.Error("[TamingController] TamedEntitySpawner service is missing.");
            return;
        }

        if (!TamedEntitySpawner.Instance.TrySpawnPrefab(
                prefab,
                position,
                rotation,
                playerNetObj.OwnerClientId,
                out TamedTag spawnedTag,
                out string spawnError))
        {
            GameLog.Error($"[TamingController] Failed to spawn tamed entity: {spawnError}");
            return;
        }

        targetNetObj.Despawn(true);

        GameEntity tamedEntity = GetGameEntity(spawnedTag.NetworkObject);
        tamedEntity?.SetOwner(playerNetObj);

        RecordTamingFact(spawnedTag);
        EventManager.Instance?.Publish(new EntityTamedEvent(player, position, name));
    }

    private void RecordTamingFact(TamedTag spawnedTag)
    {
        if (!IsServer || spawnedTag == null) return;

        if (tamedBotFactKey == null || WorldFactsService.Instance == null)
        {
            GameLog.Error("[TamingController] Cannot record taming progress: fact key or WorldFactsService is missing.");
            return;
        }

        TameableDatabase database = TamedRegistry.Instance != null ? TamedRegistry.Instance.Database : null;
        if (database == null || !database.TryGetById(spawnedTag.TameableId, out TameableDefinition definition))
        {
            GameLog.Error($"[TamingController] Cannot record taming progress: no definition for '{spawnedTag.TameableId}'.");
            return;
        }

        // Only a successful wild-to-tamed conversion awards credit. Restoring or
        // spawning a bot through other paths must not count as a new tame.
        WorldFactsService.Instance.AddInt(tamedBotFactKey.GetKey(definition.ID), 1);
    }

    private static NetworkObject GetNetworkObject(GameEntity entity)
    {
        if (entity == null)
            return null;

        return entity.NetworkObject != null
            ? entity.NetworkObject
            : entity.GetComponentInParent<NetworkObject>(true);
    }

    private static GameEntity GetGameEntity(NetworkObject networkObject)
    {
        if (networkObject == null)
            return null;

        return networkObject.GetComponent<GameEntity>() ??
               networkObject.GetComponentInChildren<GameEntity>(true);
    }

    private static Blackboard GetBlackboard(NetworkObject networkObject)
    {
        if (networkObject == null)
            return null;

        return networkObject.GetComponent<Blackboard>() ??
               networkObject.GetComponentInChildren<Blackboard>(true);
    }

    private void Log(string message)
    {
        if (logPrototypeFlow)
            GameLog.Info($"[OverridePrototype] {message}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxOverrideDistance = Mathf.Max(0.1f, maxOverrideDistance);
        holdPulseTimeout = Mathf.Max(0.2f, holdPulseTimeout);
        holdPulseInterval = Mathf.Clamp(holdPulseInterval, 0.05f, holdPulseTimeout * 0.5f);
        sessionAuditInterval = Mathf.Max(0.02f, sessionAuditInterval);
        skillCheckLeadTime = Mathf.Max(0.05f, skillCheckLeadTime);
        maxLatencyCompensation = Mathf.Max(0f, maxLatencyCompensation);
    }
#endif

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            DownedInterruptRegistry.Unregister(this);
        }

        _sessionsByClient.Clear();
        _targetClaims.Clear();
        _pendingCancellations.Clear();

        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }
}
