using Artifika.AI;
using Artifika.AI.Death.Reactions;
using NewInventorySystem;
using UnityEngine;
using Unity.Netcode;
using Sirenix.OdinInspector;

public class DropLootDeathReaction : NetworkBehaviour, IDeathReaction, IDeathReactionInitializable
{
    private EntityConfiguration entityConfig;
    private HealthComponent healthComponent;
    private bool hasDropped;
    private DropData[] DropDatas => entityConfig != null ? entityConfig.potentialDrops : null;

    [Title("Loot Drop Settings")]
    [SerializeField] private GameObject defaultDropPrefab;
    [SerializeField] private Transform lootSpawnPoint; 

    [Title("Force Settings")]
    [SerializeField] private float lootDropRadius = 1f; 
    [SerializeField] private float forceStrength = 2f; 
    [SerializeField] private float torqueStrength = 1f;

    public void Initialize(Blackboard blackboard)
    {
        entityConfig = blackboard != null && blackboard.gameEntity != null
            ? blackboard.gameEntity.entityConfiguration
            : null;

        if (healthComponent != null)
            healthComponent.OnServerDeathArgs -= HandleServerDeath;

        healthComponent = blackboard != null ? blackboard.healthComponent : null;

        if (healthComponent != null)
            healthComponent.OnServerDeathArgs += HandleServerDeath;
    }

    public void Execute()
    {
        if (!IsServer) return;
        DropLoot();
    }

    private void OnDestroy()
    {
        if (healthComponent != null)
            healthComponent.OnServerDeathArgs -= HandleServerDeath;
    }
    
    private void DropLoot()
    {
        if (hasDropped)
            return;

        DropData[] dropDatas = DropDatas;
        if (dropDatas == null || dropDatas.Length == 0 || defaultDropPrefab == null)
            return;

        hasDropped = true;

        foreach (var dropdata in dropDatas)
        {
            if (dropdata == null ||
                dropdata.item == null ||
                Random.value > dropdata.dropChance / 100f)
                continue;

            GameObject lootDrop = SpawnLootItem(defaultDropPrefab);
            if (lootDrop == null)
                continue;

            SetLoot(lootDrop, dropdata);
            ApplyForce(lootDrop);
        }
    }

    private void HandleServerDeath(DamageInstance killingBlow)
    {
        if (IsServer)
            DropLoot();
    }

    private GameObject SpawnLootItem(GameObject lootPrefab)
    {
        if (lootPrefab == null)
            return null;

        Vector3 randomOffset = Random.insideUnitSphere * lootDropRadius;
        randomOffset.y = 0; 

        Vector3 spawnPosition = lootSpawnPoint != null
            ? lootSpawnPoint.position + randomOffset
            : transform.position + randomOffset;

        GameObject lootObject = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
        var networkObject = lootObject.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(); 
        }

        return lootObject;
    }

    private void SetLoot(GameObject lootDrop, DropData dropData)
    {
        ItemPickUp itemPickUp = lootDrop.GetComponentInChildren<ItemPickUp>();

        if (itemPickUp != null)
        {
            int minDrop = Mathf.Max(1, Mathf.Min(dropData.dropRange.x, dropData.dropRange.y));
            int maxDrop = Mathf.Max(minDrop, Mathf.Max(dropData.dropRange.x, dropData.dropRange.y));
            int durability = (dropData.item.capabilities & ItemCaps.HasDurability) != 0
                ? dropData.item.maxDurability
                : 0;

            itemPickUp.SetItemServerRpc(
                (ushort)dropData.item.ID,
                (ushort)Random.Range(minDrop, maxDrop + 1),
                (ushort)Mathf.Max(0, durability));
        }
    }

    private void ApplyForce(GameObject lootDrop)
    {
        Rigidbody rb = lootDrop.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),1f, Random.Range(-1f, 1f)
            ).normalized;

            rb.AddForce(randomDirection * forceStrength, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            );
            rb.AddTorque(randomTorque * torqueStrength, ForceMode.Impulse);
        }
    }
    
}
