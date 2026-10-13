using UnityEngine;

public interface IEntityModule
{
    GameObject GameObject { get; }

    Transform Transform { get; }

    void Initialize();

    void OnModuleEnabled();

    void OnModuleDisabled();

    bool IsEnabled { get; }
}