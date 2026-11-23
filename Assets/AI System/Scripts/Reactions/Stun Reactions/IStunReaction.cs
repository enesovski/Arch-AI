namespace Archura.Stun.Reactions
{
    public interface IStunReaction
    {
        void Initialize(Blackboard blackboard);
        void OnStunnedChanged(bool isStunned);
    }
}
