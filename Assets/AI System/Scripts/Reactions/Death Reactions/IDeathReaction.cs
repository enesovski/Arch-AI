namespace Artifika.AI.Death.Reactions
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
