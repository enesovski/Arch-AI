using Artifika.AI.Death.Reactions;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
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
}
