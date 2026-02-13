using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

using Blackboard = Artifika.AI.Blackboard;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SearchForTarget", story: "[Blackboard] searches area", category: "Action", id: "a3bd44c8dee8aab5311894dcf77d8888")]
public partial class SearchForTargetAction : Action
{
    
    [SerializeReference] public BlackboardVariable<Blackboard> Blackboard;

    private MovementModule _movementModule;
    private AggroModule _aggroModule;

    public float lookAroundDuration = 2f;
    public int searchPointCount = 3;
    public float minSearchDistance = 3f;
    public float maxSearchDistance = 6f;

    private enum SearchPhase
    {
        GoingToLastSeen,
        LookingAround,
        SearchingNearby,
        Complete
    }

    private SearchPhase currentPhase;
    private float lookAroundTimer;
    private Vector3[] searchPoints;
    private int currentSearchIndex;

    protected override Status OnStart()
    {
        _movementModule = Blackboard.Value.movementModule;
        _aggroModule = Blackboard.Value.aggroModule;
        
        currentPhase = SearchPhase.GoingToLastSeen;
        lookAroundTimer = 0f;
        currentSearchIndex = 0;
        searchPoints = null;

        MovementModule movementModule = _movementModule;
        Vector3 lastSeen = _aggroModule.LastKnownPosition;

        movementModule.SetSpeed(movementModule.MovementProfile.searchSpeed);
        movementModule.MoveTo(lastSeen);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_aggroModule.CurrentAggroState != AggroState.Suspicious)
            return Status.Failure;

        switch (currentPhase)
        {
            case SearchPhase.GoingToLastSeen:
                return UpdateGoingToLastSeen();

            case SearchPhase.LookingAround:
                return UpdateLookingAround();

            case SearchPhase.SearchingNearby:
                return UpdateSearchingNearby();

            case SearchPhase.Complete:
                return Status.Success;

            default:
                return Status.Failure;
        }
    }

    private Status UpdateGoingToLastSeen()
    {
        if (_movementModule.HasArrived())
        {
            currentPhase = SearchPhase.LookingAround;
            lookAroundTimer = lookAroundDuration;
            _movementModule.SetSpeed(0f); 
        }

        return Status.Running;
    }

    private Status UpdateLookingAround()
    {
        lookAroundTimer -= Time.deltaTime;


        if (lookAroundTimer <= 0f)
        {
            Vector3 lastSeen = _aggroModule.LastKnownPosition;
            searchPoints = GenerateSearchPoints(lastSeen);

            if (searchPoints.Length > 0)
            {
                currentPhase = SearchPhase.SearchingNearby;
                currentSearchIndex = 0;

                _movementModule.SetSpeed(_movementModule.MovementProfile.searchSpeed);
                _movementModule.MoveTo(searchPoints[0]);
            }
            else
            {
                currentPhase = SearchPhase.Complete;
                return Status.Success;
            }
        }

        return Status.Running;
    }

    private Status UpdateSearchingNearby()
    {
        if (_movementModule.HasArrived())
        {
            currentSearchIndex++;

            if (currentSearchIndex >= searchPoints.Length)
            {
                currentPhase = SearchPhase.Complete;
                return Status.Success;
            }
            else
            {
                _movementModule.MoveTo(searchPoints[currentSearchIndex]);
            }
        }

        return Status.Running;
    }

    private Vector3[] GenerateSearchPoints(Vector3 center)
    {
        System.Collections.Generic.List<Vector3> validPoints = new System.Collections.Generic.List<Vector3>();

        float angleStep = 360f / searchPointCount;

        for (int i = 0; i < searchPointCount; i++)
        {
            float angle = angleStep * i + UnityEngine.Random.Range(-20f, 20f); 
            float distance = UnityEngine.Random.Range(minSearchDistance, maxSearchDistance);

            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            Vector3 searchPoint = center + offset;

            validPoints.Add(searchPoint);
        }

        return validPoints.ToArray();
    }

    protected override void OnEnd()
    {
        _movementModule.Stop();
    }
}