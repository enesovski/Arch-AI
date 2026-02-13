using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "AI/Movement Data")]
public class MovementProfile : ScriptableObject
{
    [Title("Flee Settings")]
    public float fleeDistance = 8f;
    public float fleeSpeed = 12f;

    [Title("Wander Settings")]
    public float nestRadius = 10f;
    public float wanderSpeed = 6f;
    public float minWanderDistance = 1f;
    public float maxWanderDistance = 5f;

    [Title("Chase Settings")]
    public float chaseSpeed = 8f;
    public float flankAngleMax = 45;

    [Title("Search Settings")]
    public float searchRadius = 10f;
    public float searchSpeed = 6f;
    public float minSearchDistance = 1f;
    public float maxSearchDistance = 5f;

    [Title("Combat State Settings")]
    public float combatDistance = 20f;

    [Title("Min and Max Speed")]
    [ShowInInspector, ReadOnly]
    public float MinPossibleSpeed => CalculateMinSpeed();

    [ShowInInspector, ReadOnly]
    public float MaxPossibleSpeed => CalculateMaxSpeed();

    private float CalculateMinSpeed()
    {
        float[] candidates = new float[]
        {
            wanderSpeed, searchSpeed, chaseSpeed, fleeSpeed
        }.Where(s => s > 0f).ToArray();

        if (candidates.Length == 0) return 1f;

        float minState = candidates.Min();
        return Mathf.Max(0.01f, minState);
    }

    private float CalculateMaxSpeed()
    {
        float[] candidates = new float[]
        {
            wanderSpeed, searchSpeed, chaseSpeed, fleeSpeed
        }.Where(s => s > 0f).ToArray();

        if (candidates.Length == 0) return 2f;

        float maxState = candidates.Max();
        float min = CalculateMinSpeed();
        return Mathf.Max(min + 0.01f, maxState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif 


}
