using System.Collections.Generic;
using Artifika.AI;
using Artifika.AI.Death.Reactions;
using UnityEngine;
using Unity.Netcode;
using Sirenix.OdinInspector;

public class ExplosionDeathReaction : NetworkBehaviour, IDeathReaction
{
    [Title("Body Parts Parent")]
    [SerializeField] private GameObject bodyPartsPrefab;

    [Title("Explosion Settings")]   
    [SerializeField] private Transform bodySpawnPoint;
    [SerializeField] private float force = 18f;
    [SerializeField] private float radius = 4.0f;
    [SerializeField] private float upwardsModifier = 0.35f;
    [SerializeField] private float randomTorque = 8f;

    public void Initialize(Blackboard blackboard)
    {
        
    }

    public void Execute()
    {
        ExplodeBodyParts();
    }
    
    public void ExplodeBodyParts()
    {
        if (IsServer)
        {
            SpawnBodyPartsClientRpc();
            Destroy(this);
        }
        else
        {
            SpawnBodyPartsServerRpc();
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void SpawnBodyPartsServerRpc()
    {
        SpawnBodyPartsClientRpc();
    }

    [ClientRpc]
    private void SpawnBodyPartsClientRpc()
    {
        SpawnDismemberedBody();
    }
    
    private void SpawnDismemberedBody()
    {
        if (!bodyPartsPrefab)
            return;

        GameObject parent = Instantiate(bodyPartsPrefab, transform.position, Quaternion.LookRotation(bodySpawnPoint.forward, Vector3.up));

        Rigidbody[] partBodies = parent.GetComponentsInChildren<Rigidbody>(includeInactive: false);
        HashSet<Rigidbody> partSet = new HashSet<Rigidbody>(partBodies);

        foreach (Rigidbody rb in partBodies)
        {
            if (rb == null) 
                continue;

            rb.AddExplosionForce(force, bodySpawnPoint.position, radius, upwardsModifier, ForceMode.Impulse);

            Vector3 torque = Random.insideUnitSphere * randomTorque;
            rb.AddTorque(torque, ForceMode.Impulse);
        }

    }
    
}
