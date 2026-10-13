using System;
using System.Collections.Generic;
using Artifika.States;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class TamingController : NetworkBehaviour
    {
        public static TamingController Instance { get; private set; }

        [FormerlySerializedAs("maxOverrideDistance")]
        [SerializeField, Min(0.1f)] private float maxTamingDistance = 4f;
        [SerializeField] private DynamicWorldFactKey tamedBotFactKey;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                GameLog.Warning("[TamingController] Multiple instances found. Keeping the first instance.");
                return;
            }

            Instance = this;
        }

        public bool TryTameServer(ulong clientId, NetworkObject playerObject, GameEntity target)
        {
            if (!IsServer || NetworkManager == null)
                return false;

            if (!TryValidateTaming(clientId, playerObject, target, out GameEntity player, out GameEntity targetEntity))
                return false;

            return SpawnTamed(player, targetEntity);
        }

        private bool TryValidateTaming(
            ulong clientId,
            NetworkObject playerObject,
            GameEntity requestedTarget,
            out GameEntity player,
            out GameEntity target)
        {
            player = null;
            target = null;

            if (playerObject == null || !playerObject.IsSpawned || playerObject.OwnerClientId != clientId)
                return false;

            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
                client.PlayerObject == null ||
                client.PlayerObject.NetworkObjectId != playerObject.NetworkObjectId)
                return false;

            NetworkObject targetObject = GetNetworkObject(requestedTarget);
            if (targetObject == null || !targetObject.IsSpawned)
                return false;

            Blackboard targetBlackboard = GetBlackboard(targetObject);
            if (targetBlackboard == null || !targetBlackboard.CanBeTamed())
                return false;

            if (!IsWithinTamingDistance(playerObject, targetObject, GetTargetHitboxes(targetBlackboard)))
                return false;

            PlayerStateMachine playerStateMachine = playerObject.GetComponentInChildren<PlayerStateMachine>(true);
            if (playerStateMachine == null || !playerStateMachine.HasPermission(PlayerPermission.Attack))
                return false;

            player = GetGameEntity(playerObject);
            target = targetBlackboard.gameEntity != null ? targetBlackboard.gameEntity : GetGameEntity(targetObject);
            return player != null && target != null;
        }

        private static Collider[] GetTargetHitboxes(Blackboard targetBlackboard)
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

        private bool IsWithinTamingDistance(
            NetworkObject playerObject,
            NetworkObject targetObject,
            Collider[] targetHitboxes)
        {
            float maxDistance = Mathf.Max(0.1f, maxTamingDistance);
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

        private bool SpawnTamed(GameEntity player, GameEntity target)
        {
            if (!IsServer) return false;

            GameObject prefab = target.entityConfiguration != null ? target.entityConfiguration.tamedPrefab : null;
            NetworkObject playerNetObj = GetNetworkObject(player);
            NetworkObject targetNetObj = GetNetworkObject(target);

            if (prefab == null || playerNetObj == null || targetNetObj == null)
            {
                GameLog.Warning($"[TamingController] Cannot spawn tamed entity. Prefab:{prefab != null} " +
                    $"PlayerNetObj:{playerNetObj != null} TargetNetObj:{targetNetObj != null}");
                return false;
            }

            Vector3 position = target.transform.position;
            Quaternion rotation = target.transform.rotation;
            string name = target.entityConfiguration != null ? target.entityConfiguration.entityName : target.name;

            if (TamedEntitySpawner.Instance == null)
            {
                GameLog.Error("[TamingController] TamedEntitySpawner service is missing.");
                return false;
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
                return false;
            }

            targetNetObj.Despawn(true);

            GameEntity tamedEntity = GetGameEntity(spawnedTag.NetworkObject);
            tamedEntity?.SetOwner(playerNetObj);

            RecordTamingFact(spawnedTag);
            EventManager.Instance?.Publish(new EntityTamedEvent(player, position, name));
            return true;
        }

        private void RecordTamingFact(TamedTag spawnedTag)
        {
            if (!IsServer || spawnedTag == null) return;

            if (tamedBotFactKey == null || WorldFactsService.Instance == null)
            {
                GameLog.Error(
                    "[TamingController] Cannot record taming progress: fact key or WorldFactsService is missing.");
                return;
            }

            TameableDatabase database = TamedRegistry.Instance != null ? TamedRegistry.Instance.Database : null;
            if (database == null || !database.TryGetById(spawnedTag.TameableId, out TameableDefinition definition))
            {
                GameLog.Error(
                    $"[TamingController] Cannot record taming progress: no definition for '{spawnedTag.TameableId}'.");
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxTamingDistance = Mathf.Max(0.1f, maxTamingDistance);
        }
#endif

        public override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            base.OnDestroy();
        }
    }
}
