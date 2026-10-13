using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Attack
{
    [CreateAssetMenu(fileName = "Combat Configuration", menuName = "AI/Combat/Combat Configuration")]
    public sealed class CombatConfiguration : ScriptableObject
    {
        [Title("Ranges")]
        [MinValue(0f)]
        [SerializeField] private float combatRadius = 10f;

        [MinValue(0f)]
        [SerializeField] private float closeCombatRadius = 3f;

        public float CombatRadius => combatRadius;
        public float CloseCombatRadius => closeCombatRadius;
    }
}
