namespace Archura.Death.Reactions
{
    public interface IDeathReaction
    {
        void Execute();
    }

    public interface IDeathReactionInitializable
    {
        void Initialize(Blackboard blackboard);
    }
}
