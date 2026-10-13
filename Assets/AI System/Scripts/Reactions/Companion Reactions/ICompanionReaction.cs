using Artifika.AI;

public interface ICompanionReaction : IReaction
{
    void OnCompanionStateChanged(CompanionState previousState, CompanionState nextState);
}