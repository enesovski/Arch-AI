public readonly struct AggroStateChangeEventArgs
{
    public AggroState PreviousState { get; }
    public AggroState NewState { get; }

    public AggroStateChangeEventArgs(
        AggroState previousState,
        AggroState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}