using UnityEngine;

namespace Artifika.AI
{
    public struct TacticalContext
    {
        public Vector3 SelfPosition;
        public Vector3 TargetPosition;
        public bool HasTarget;
        public float Distance;            
        public float NormalizedDistance;  

        public float OwnHealthNormalized;        
        public float TargetHealthNormalized;     

        public float ThreatScore;        

        public bool CanAttack;            
        public bool CanStartAttack;
        public bool HasLineOfSight;
        public float TimeSinceLastAttack; 

        public int TargetAggroCount;
        public int MaxExpectedAggro;

        public float Aggressiveness;
    }
}
