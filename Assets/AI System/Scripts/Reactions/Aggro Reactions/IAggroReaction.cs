namespace Archura.Aggro.Reactions
{
    public interface IAggroReaction : IReaction
    {
        void OnEngagementChanged(EngagementChangeEventArgs args);
    }
}
