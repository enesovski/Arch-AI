using Sirenix.OdinInspector;
using UnityEngine;

namespace Archura
{
    [CreateAssetMenu(fileName = "Ranged Combat Profile", menuName = "AI/Combat/Ranged Combat Profile")]
    public sealed class RangedCombatProfile : ScriptableObject
    {
        [MinMaxSlider(1f, 50f, true)]
        [Tooltip("Preferred horizontal distance (m) between the target's root and the agent. Not a damage range; weapon range is checked separately. May fire from closer when no suitable spot is found.")]
        public Vector2 preferredDistanceRange = new(12f, 20f);

        [Min(0f), Tooltip("Starts a short evasion step when the target comes within this horizontal distance. The goal is not extended mid-step. If the distance cannot be reached, the agent may hold briefly and answer with a suitable close attack.")]
        public float retreatTriggerDistance = 8f;

        [Min(0f), Tooltip("Seconds spent standing and aiming after repositioning before firing; weapon cooldown can extend it. The agent does not move during the shot animation.")]
        public float aimPause = 0.35f;

        [Min(0.2f), Tooltip("Maximum time (s) for one side/back step. When it expires on a blocked path the move is dropped: fires from the current spot if the shot is clear, otherwise keeps looking for a new position.")]
        public float repositionTimeout = 2.5f;

        [FoldoutGroup("Advanced")]
        [Tooltip("Collider layers that block the shot between muzzle and the target's aim point (include terrain, ground, structures and walls). Triggers, the agent itself and the target's colliders are ignored. Independent of the visual sensor mask.")]
        public LayerMask shootingObstacleMask = Physics.DefaultRaycastLayers;

        public float MinimumDistance => Mathf.Max(1f, Mathf.Min(preferredDistanceRange.x, preferredDistanceRange.y));
        public float MaximumDistance => Mathf.Max(MinimumDistance, Mathf.Max(preferredDistanceRange.x, preferredDistanceRange.y));
    }
}
