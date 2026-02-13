using System;
using UnityEngine;

namespace Artifika.AI.Sensors
{
    public interface ISensor
    {
        void Initialize(Transform owner);
        void Detect();
        event Action<GameEntity, float> OnDetected;
    }
}
