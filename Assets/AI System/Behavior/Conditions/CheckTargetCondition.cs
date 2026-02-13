using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckTarget", story: "Check if [Target] is [Equal] [Boolean]", category: "Conditions", id: "e773e8c29ef1bd90cbdebaf2ef02e742")]
public partial class CheckTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameEntity> Target;
    [Comparison(comparisonType: ComparisonType.Boolean)]
    [SerializeReference] public BlackboardVariable<ConditionOperator> Equal;
    [SerializeReference] public BlackboardVariable<bool> Boolean;

    public override bool IsTrue()
    {
        if(Target.Value != null)
        {
            return true;
        }

        return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
