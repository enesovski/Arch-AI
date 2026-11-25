namespace Archura.Aggro
{
    public static class EngagementStateExtensions
    {
        public static bool IsSuspicious(this EngagementState state)
        {
            return state == EngagementState.Alerted || state == EngagementState.Searching;
        }

        public static bool IsEngaged(this EngagementState state)
        {
            return state == EngagementState.Engaged;
        }
    }
}
