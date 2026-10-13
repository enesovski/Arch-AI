namespace Artifika.AI.Aggro.Reactions
{
    public interface IAggroReaction : IReaction
    {
        void OnAggroStateChanged(AggroStateChangeEventArgs args);
    }
}