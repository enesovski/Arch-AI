using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public static class MovementPointPicker
    {
        private static readonly int areaMask = NavMesh.AllAreas;
        private const int MAX_ATTEMPTS = 10;
        private const float DEFAULT_SAMPLE_RADIUS = 2f;

        [ThreadStatic]
        private static NavMeshPath reusablePath;

        private static NavMeshPath ReusablePath
        {
            get
            {
                if (reusablePath == null)
                    reusablePath = new NavMeshPath();
                return reusablePath;
            }
        }

        public static bool IsPathReachable(Vector3 sourcePos, Vector3 targetPos)
        {
            NavMeshPath path = ReusablePath;
            path.ClearCorners();

            if (!NavMesh.SamplePosition(sourcePos, out NavMeshHit sourceHit, DEFAULT_SAMPLE_RADIUS, areaMask))
                return false;

            if (!NavMesh.SamplePosition(targetPos, out NavMeshHit targetHit, DEFAULT_SAMPLE_RADIUS, areaMask))
                return false;

            if (!NavMesh.CalculatePath(sourceHit.position, targetHit.position, areaMask, path))
                return false;

            return path.status == NavMeshPathStatus.PathComplete;
        }

        private static bool TryGetValidNavMeshPoint(Vector3 agentPos, Vector3 candidate, float sampleRadius, out Vector3 result)
        {
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, areaMask) &&
                IsPathReachable(agentPos, hit.position))
            {
                result = hit.position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        public static bool TryPickWanderPoint(Transform agentTransform, Vector3 basePosition, float nestRadius,
            float minDistance, float maxDistance, out Vector3 result)
        {
            float sqrNest = nestRadius * nestRadius;
            Vector3 agentPos = agentTransform.position;

            for (int i = 0; i < MAX_ATTEMPTS; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                float dist = UnityEngine.Random.Range(minDistance, maxDistance);
                Vector3 candidate = agentPos + offset * dist;

                if ((candidate - basePosition).sqrMagnitude > sqrNest)
                    continue;

                if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
                    return true;
            }

            Vector3 dirToNest = (basePosition - agentPos).normalized;
            float fallbackDist = Mathf.Min(maxDistance, nestRadius * 0.5f);
            Vector3 fallback = agentPos + dirToNest * fallbackDist;

            if (TryGetValidNavMeshPoint(agentPos, fallback, DEFAULT_SAMPLE_RADIUS, out result))
                return true;

            result = agentPos;
            return false;
        }

        public static bool TryPickChasePoint(Transform agentTransform, Transform targetTransform,
            out Vector3 result)
        {
            Vector3 agentPos = agentTransform.position;
            Vector3 chasePoint = targetTransform.position;

            if (TryGetValidNavMeshPoint(agentPos, chasePoint, DEFAULT_SAMPLE_RADIUS, out result))
                return true;

            if (NavMesh.SamplePosition(chasePoint, out NavMeshHit hit, 5f, areaMask))
            {
                result = hit.position;
                return true;
            }

            result = agentPos;
            return false;
        }

        private static readonly float[] fleeAngleMagnitudes = { 30f, 60f, 90f, 120f, 150f, 180f };
        private static readonly float[] fleeDistanceFallbacks = { 0.75f, 0.5f, 0.25f };
        private static readonly float[] flankAngles = { 90f, -90f, 60f, -60f, 120f, -120f };
        private static readonly float[] repositionAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f };

        public static bool TryPickFleePoint(Vector3 agentPos, Vector3 threatPos, float fleeDistance,
            float preferredSide, out Vector3 result, out float chosenSide)
        {
            Vector3 fleeDir = agentPos - threatPos;
            fleeDir.y = 0f;
            fleeDir = fleeDir.sqrMagnitude > 0.01f ? fleeDir.normalized : Vector3.forward;

            float sideSign = preferredSide >= 0f ? 1f : -1f;

            if (TryFleeCandidate(agentPos, fleeDir, 0f, fleeDistance, out result))
            {
                chosenSide = 0f;
                return true;
            }

            foreach (float magnitude in fleeAngleMagnitudes)
            {
                if (TryFleeCandidate(agentPos, fleeDir, magnitude * sideSign, fleeDistance, out result))
                {
                    chosenSide = sideSign;
                    return true;
                }

                if (TryFleeCandidate(agentPos, fleeDir, -magnitude * sideSign, fleeDistance, out result))
                {
                    chosenSide = -sideSign;
                    return true;
                }
            }

            foreach (float distanceMultiplier in fleeDistanceFallbacks)
            {
                float shorterDistance = fleeDistance * distanceMultiplier;

                if (TryFleeCandidate(agentPos, fleeDir, 0f, shorterDistance, out result))
                {
                    chosenSide = 0f;
                    return true;
                }

                foreach (float magnitude in fleeAngleMagnitudes)
                {
                    if (TryFleeCandidate(agentPos, fleeDir, magnitude * sideSign, shorterDistance, out result))
                    {
                        chosenSide = sideSign;
                        return true;
                    }

                    if (TryFleeCandidate(agentPos, fleeDir, -magnitude * sideSign, shorterDistance, out result))
                    {
                        chosenSide = -sideSign;
                        return true;
                    }
                }
            }

            if (NavMesh.SamplePosition(agentPos, out NavMeshHit nearHit, fleeDistance, areaMask))
            {
                Vector3 awayDir = nearHit.position - threatPos;
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude > 0.1f && Vector3.Dot(awayDir.normalized, fleeDir) > 0f)
                {
                    result = nearHit.position;
                    chosenSide = preferredSide;
                    return true;
                }
            }

            result = Vector3.zero;
            chosenSide = 0f;
            return false;
        }

        private static bool TryFleeCandidate(Vector3 agentPos, Vector3 baseDir, float angleDegrees, float distance, out Vector3 result)
        {
            Vector3 dir = angleDegrees == 0f ? baseDir : Quaternion.Euler(0f, angleDegrees, 0f) * baseDir;
            Vector3 candidate = agentPos + dir * distance;
            return TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result);
        }

        public static bool TryPickFlankPoint(Vector3 agentPos, Vector3 targetPos, float flankDistance, out Vector3 result)
        {
            Vector3 toTarget = targetPos - agentPos;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.01f)
                toTarget = Vector3.forward;

            Vector3 dirToTarget = toTarget.normalized;

            foreach (float angle in flankAngles)
            {
                Vector3 flankDir = Quaternion.Euler(0f, angle, 0f) * dirToTarget;
                Vector3 candidate = targetPos + flankDir * flankDistance;

                if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
                    return true;
            }

            result = Vector3.zero;
            return false;
        }

        public static bool TryPickRepositionPoint(Vector3 agentPos, Vector3 targetPos, float desiredDistance, out Vector3 result)
        {
            Vector3 toTarget = targetPos - agentPos;
            toTarget.y = 0f;
            float currentDistance = toTarget.magnitude;

            if (currentDistance < 0.1f)
                toTarget = Vector3.forward * desiredDistance;

            Vector3 dirToTarget = toTarget.normalized;

            float distanceDiff = desiredDistance - currentDistance;

            // Check nearby angles first to avoid unnecessary detours.
            foreach (float angle in repositionAngles)
            {
                Vector3 targetToAgentDir = -dirToTarget;
                Vector3 rotatedDir = Quaternion.Euler(0f, angle, 0f) * targetToAgentDir;

                Vector3 candidate = targetPos + rotatedDir * desiredDistance;

                if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
                    return true;
            }

            Vector3 directCandidate = agentPos - dirToTarget * distanceDiff;
            if (TryGetValidNavMeshPoint(agentPos, directCandidate, DEFAULT_SAMPLE_RADIUS, out result))
                return true;

            result = Vector3.zero;
            return false;
        }

        public static bool TryPickStrafePoint(Vector3 agentPos, Vector3 targetPos, float strafeDistance, bool strafeRight, out Vector3 result)
        {
            Vector3 toTarget = targetPos - agentPos;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.01f)
                toTarget = Vector3.forward;

            Vector3 dirToTarget = toTarget.normalized;

            Vector3 strafeDir = strafeRight ? Vector3.Cross(Vector3.up, dirToTarget) : Vector3.Cross(dirToTarget, Vector3.up);

            Vector3 candidate = agentPos + strafeDir * strafeDistance;

            if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
                return true;

            Vector3 oppositeDir = -strafeDir;
            Vector3 oppositeCandidate = agentPos + oppositeDir * strafeDistance;

            if (TryGetValidNavMeshPoint(agentPos, oppositeCandidate, DEFAULT_SAMPLE_RADIUS, out result))
                return true;

            result = Vector3.zero;
            return false;
        }
    }
}
