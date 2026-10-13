namespace Artifika.AI.Aggro
{
    public enum EngagementState
    {
        Passive,    //no threat detected
        Alerted,    // initial awareness, not yet committed
        Searching,  // post-combat, target lost
        Engaged     // full commitment
    }
}
