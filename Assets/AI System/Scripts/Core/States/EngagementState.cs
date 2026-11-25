using Unity.Behavior;

namespace Archura.Aggro
{
    [BlackboardEnum]
    public enum EngagementState
    {
        Passive = 0,
        Alerted = 1,
        Searching = 2,
        Engaged = 3
    }
}
