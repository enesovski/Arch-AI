using Artifika.AI;
using Artifika.AI.Death.Reactions;using UnityEngine;
using Unity.Netcode;
using NewInventorySystem;
using Sirenix.OdinInspector;

public class DropLootDeathReaction : NetworkBehaviour, IDeathReaction
{
    [Title("Entity Data")]
    [SerializeField] private BaseAnimalData animalData;
    private DropData[] dropDatas => animalData.potentialDrops;

    [Title("Loot Drop Settings")]
    [SerializeField] private GameObject defaultDropPrefab;
    [SerializeField] private Transform lootSpawnPoint; 

    [Title("Force Settings")]
    [SerializeField] private float lootDropRadius = 1f; 
    [SerializeField] private float forceStrength = 2f; 
    [SerializeField] private float torqueStrength = 1f; 
    
    public void Initialize(Blackboard blackboard) { }

    public void Execute()
    {
        if (!IsServer) return;
        DropLoot();
    }
    
    private void DropLoot()
    {
        foreach (var dropdata in dropDatas)
        {
            if (Random.value <= dropdata.dropChance / 100) 
            {
                GameObject lootDrop = SpawnLootItem(defaultDropPrefab);

                SetLoot(lootDrop, dropdata.itemData);

                ApplyForce(lootDrop);
            }
        }
    }

    private GameObject SpawnLootItem(GameObject lootPrefab)
    {
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
        else
        {
            GameLog.Error($"Loot prefab {lootPrefab.name} does not have a NetworkObject component attached!");
        }

        return lootObject;
    }

    private void SetLoot(GameObject lootDrop, ItemEntry loot)
    {
        ItemPickUp itemPickUp = lootDrop.GetComponentInChildren<ItemPickUp>();

        if (itemPickUp != null)
        {
            itemPickUp.SetItemServerRpc((ushort)loot.itemSO.ID,(ushort)loot.quantity, (ushort)loot.durability);
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
