using System;

namespace Archura
{
    /// <summary>
    /// Health as seen by the AI. Implement it on the host game's health component
    /// (or on a thin adapter next to it); every module resolves health through this
    /// contract instead of a concrete game type.
    /// </summary>
    public interface IEntityHealth
    {
        float Health { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }

        /// <summary>Raised on every peer when the entity dies.</summary>
        event Action OnDeath;

        /// <summary>Raised on the server only, once the death is authoritative.</summary>
        event Action OnServerDeath;

        /// <summary>Raised whenever damage is applied to this entity.</summary>
        event Action<DamageReport> OnDamageReceived;
    }
}
