namespace Archura.Utility
{
    public interface IUtilityActionNode
    {
        float EvaluateScore(TacticalContext ctx);
        string GetActionName();
    }
}
