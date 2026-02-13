using System;
using Unity.Behavior;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Out Of Nest Radius", story: "[Blackboard] is out of nest", category: "Conditions", id: "467a93e782f91997930121088b46b6cd")]
public partial class IsOutOfNestCondition : Condition
{

    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    private MovementModule _movementModule;
    private float _arriveTolerance = 0.5f;
    private float _nestRadius;
    public override bool IsTrue()
    {
        _movementModule = Blackboard.Value.movementModule;
        _nestRadius = Blackboard.Value.nestRadius;
        Vector3 nest = _movementModule.SpawnPoint;
        float sqrDist = (_movementModule.transform.position - nest).sqrMagnitude;
        if (_nestRadius > 0f && sqrDist <= (_nestRadius + _arriveTolerance) * (_nestRadius + _arriveTolerance))
        {
            return false;
        }
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
