namespace Archura.Aggro.Reactions
{
    public interface IAggroReaction : IReaction
    {
        void OnAggroStateChanged(AggroStateChangeEventArgs args);
    }
}
