using UnityEngine;

namespace Archura
{
    public sealed class DestinationThrottle
    {
        private Vector3 lastDestination;
        private bool hasDestination;

        public void Reset()
        {
            hasDestination = false;
        }

        public bool ShouldMoveTo(Vector3 point, float thresholdSqr)
        {
            if (hasDestination && (lastDestination - point).sqrMagnitude <= thresholdSqr)
                return false;

            lastDestination = point;
            hasDestination = true;
            return true;
        }
    }
}
