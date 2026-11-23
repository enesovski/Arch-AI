using System;

namespace Archura
{
    /// <summary>
    /// Stun status from the host's stats/status-effect system. Read by conditions and
    /// the stun reaction bus.
    /// </summary>
    public interface IStunState
    {
        bool IsStunned { get; }
        event Action<bool> OnStunnedChanged;
    }
}
