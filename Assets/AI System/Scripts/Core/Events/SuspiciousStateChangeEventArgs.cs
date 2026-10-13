namespace Artifika.AI.Aggro
{
    public readonly struct SuspiciousStateChangeEventArgs
    {
        public readonly SuspiciousState PreviousState;
        public readonly SuspiciousState NewState;

        public SuspiciousStateChangeEventArgs(SuspiciousState previous, SuspiciousState next)
        {
            PreviousState = previous;
            NewState = next;
        }
    }
}