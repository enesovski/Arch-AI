using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Attack
{
    [CreateAssetMenu(fileName = "RangedAttackDefinition", menuName = "AI/Attacks/RangedAttackDefinition")]
    public class RangedAttackDefinition : BaseAttackDefinition
    {
        [Title("Projectile Settings")]
        public ProjectileMovementType projectileMovementType;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int projectileCount = 1;

        [Space]
        [SerializeField] private float spreadAngle = 0f;
        [SerializeField] private float spreadRadius = 0f;

        public GameObject ProjectilePrefab => projectilePrefab;
        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public float SpreadRadius => spreadRadius;
    }

    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public enum ProjectileMovementType
    {
        Linear,
        Parabolic,
        Static
    }
}
