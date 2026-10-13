using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class EntitySpawnAnchor : NetworkBehaviour
    {
        [FormerlySerializedAs("bossPrefab")]
        [SerializeField] private GameObject entityPrefab;

        private readonly NetworkVariable<NetworkObjectReference> spawnedEntityReference = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public GameObject SpawnedEntity { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            spawnedEntityReference.OnValueChanged += HandleSpawnedEntityReferenceChanged;
            ResolveSpawnedEntity(spawnedEntityReference.Value);

            if (IsServer)
                StartCoroutine(ServerSpawnWhenReady());
        }

        public override void OnNetworkDespawn()
        {
            spawnedEntityReference.OnValueChanged -= HandleSpawnedEntityReferenceChanged;
            base.OnNetworkDespawn();
        }

        private IEnumerator ServerSpawnWhenReady()
        {
            yield return WaitForSpawnReadiness();

            if (!ShouldSpawnEntity())
                yield break;

            SpawnEntity();
        }

        protected virtual IEnumerator WaitForSpawnReadiness()
        {
            yield break;
        }

        protected virtual bool ShouldSpawnEntity() => true;

        private void SpawnEntity()
        {
            if (entityPrefab == null)
            {
                GameLog.Error($"[{GetType().Name}] No entity prefab assigned on {name}.", this);
                return;
            }

            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject entity = Instantiate(entityPrefab, spawnPos, spawnRot);

            if (!entity.TryGetComponent(out NetworkObject netObj))
            {
                GameLog.Error($"[{GetType().Name}] Spawned prefab '{entityPrefab.name}' has no NetworkObject.", entity);
                Destroy(entity);
                return;
            }

            netObj.Spawn();
            spawnedEntityReference.Value = netObj;
            SetSpawnedEntity(entity);

            if (entity.TryGetComponent(out MovementModule movement))
                movement.SetSpawnPoint(spawnPos);
        }

        private void HandleSpawnedEntityReferenceChanged(NetworkObjectReference previous, NetworkObjectReference current)
        {
            ResolveSpawnedEntity(current);
        }

        private void ResolveSpawnedEntity(NetworkObjectReference reference)
        {
            if (reference.TryGet(out NetworkObject netObj))
                SetSpawnedEntity(netObj.gameObject);
        }

        private void SetSpawnedEntity(GameObject entity)
        {
            if (SpawnedEntity == entity)
                return;

            SpawnedEntity = entity;
        }
    }
}
