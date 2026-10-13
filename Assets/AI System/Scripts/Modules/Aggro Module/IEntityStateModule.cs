using System;

public interface IEntityStateModule
{
        GameEntity CurrentTarget { get; }
        event Action<TargetChangeEventArgs> OnTargetChanged;
}
