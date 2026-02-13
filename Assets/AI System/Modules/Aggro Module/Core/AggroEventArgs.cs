public readonly struct AggroStateChangeEventArgs
{
    public AggroState PreviousState { get; }
    public AggroState NewState { get; }
    public GameEntity Target { get; }

    public AggroStateChangeEventArgs(
        AggroState previousState,
        AggroState newState,
        GameEntity target)
    {
        PreviousState = previousState;
        NewState = newState;
        Target = target;
    }
}
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
