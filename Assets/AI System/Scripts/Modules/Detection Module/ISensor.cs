using System;

namespace Archura.Sensors
{
    public interface ISensor
    {
        void Initialize(GameEntity owner);
        void Detect();
        event Action<GameEntity, float> OnDetected;
    }
}
