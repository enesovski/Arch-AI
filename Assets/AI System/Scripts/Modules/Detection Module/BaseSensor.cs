using System;
using UnityEngine;

namespace Artifika.AI.Sensors
{
    public abstract class BaseSensor : MonoBehaviour, ISensor
    {
        [SerializeField] protected float strengthMultiplier = 1f;

        public event Action<GameEntity, float> OnDetected;
        protected GameEntity owner;

        public virtual void Initialize(GameEntity owner)
        {
            this.owner = owner;
        }

        protected void Emit(GameEntity source, float rawStrength)
        {
            OnDetected?.Invoke(source, rawStrength * strengthMultiplier);
        }

        public abstract void Detect();
    }
}
