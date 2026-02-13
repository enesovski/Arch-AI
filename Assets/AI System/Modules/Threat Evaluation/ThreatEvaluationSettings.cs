using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Threat Evaluation Settings", menuName = "AI/Threats/Threat Evaluation Settings")]
public class ThreatEvaluationSettings : ScriptableObject
{
    [Title("Threshold Values")]
    public float aggroThreshold = 0.5f;
    public float suspiciousThreshold = 0.2f;

    [Header("Hit Settings")]
    public float hitMultiplier = 2f;
    public float hitDecayRate = 0.2f;

    [Title("Memory")]
    public float memoryDecayStartTime = 3f;
    public float memoryDecayDuration = 5f;

}
