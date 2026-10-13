using System;

namespace Artifika.AI.Sensors
{
    public interface ISensor
    {
        void Initialize(GameEntity owner);
        void Detect();
        event Action<GameEntity, float> OnDetected;
    }
}
