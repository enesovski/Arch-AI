using Unity.Netcode;
using UnityEngine;

namespace Archura
{
    /// <summary>
    /// Implemented by the projectile prefabs a <c>RangedAttackDefinition</c> spawns.
    /// </summary>
    public interface IProjectile
    {
        void Initialize(Vector3 targetPosition, NetworkObject owner);
    }
}
