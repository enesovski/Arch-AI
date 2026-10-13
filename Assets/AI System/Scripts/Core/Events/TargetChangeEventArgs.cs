public readonly struct TargetChangeEventArgs
{
    public GameEntity PreviousTarget { get; }
    public GameEntity NewTarget { get; }

    public TargetChangeEventArgs(
        GameEntity previousTarget,
        GameEntity newTarget)
    {
        PreviousTarget = previousTarget;
        NewTarget = newTarget;
    }
}