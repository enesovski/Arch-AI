namespace Archura.Aggro
{
    public readonly struct EngagementChangeEventArgs
    {
        public EngagementState Previous { get; }
        public EngagementState Current { get; }

        public bool ChangesAwareness =>
            Previous.IsSuspicious() != Current.IsSuspicious() || Previous.IsEngaged() != Current.IsEngaged();

        public EngagementChangeEventArgs(EngagementState previous, EngagementState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
