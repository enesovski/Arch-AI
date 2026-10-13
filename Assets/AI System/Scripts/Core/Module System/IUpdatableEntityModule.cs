using UnityEngine;

public interface IUpdatableEntityModule : IEntityModule
{
    void PerformUpdate(float deltaTime);
}