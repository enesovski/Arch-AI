using System;
using UnityEngine;
using UnityEngine.AI;

public static class MovementPointPicker
{
    private static readonly int AreaMask = NavMesh.AllAreas;
    private const int MAX_ATTEMPTS = 10;
    private const float DEFAULT_SAMPLE_RADIUS = 2f;

    [ThreadStatic]
    private static NavMeshPath _reusablePath;

    private static NavMeshPath ReusablePath
    {
        get
        {
            if (_reusablePath == null)
                _reusablePath = new NavMeshPath();
            return _reusablePath;
        }
    }

    public static bool IsPathReachable(Vector3 sourcePos, Vector3 targetPos)
    {
        NavMeshPath path = ReusablePath;
        path.ClearCorners();

        if (!NavMesh.SamplePosition(sourcePos, out NavMeshHit sourceHit, DEFAULT_SAMPLE_RADIUS, AreaMask))
            return false;

        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit targetHit, DEFAULT_SAMPLE_RADIUS, AreaMask))
            return false;

        if (!NavMesh.CalculatePath(sourceHit.position, targetHit.position, AreaMask, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private static bool TryGetValidNavMeshPoint(Vector3 agentPos, Vector3 candidate, float sampleRadius, out Vector3 result)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, AreaMask))
        {
            if (IsPathReachable(agentPos, hit.position))
            {
                result = hit.position;
                return true;
            }
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

    public static bool ComputeInterceptPoint(Vector3 shooterPos, float shooterSpeed,
        Vector3 targetPos, Vector3 targetVelocity, out Vector3 interceptPoint)
    {
        Vector3 toTarget = targetPos - shooterPos;
        float v2 = Vector3.Dot(targetVelocity, targetVelocity);
        float s2 = shooterSpeed * shooterSpeed;

        float a = v2 - s2;
        float b = 2f * Vector3.Dot(toTarget, targetVelocity);
        float c = Vector3.Dot(toTarget, toTarget);

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f || Mathf.Approximately(a, 0f))
        {
            interceptPoint = targetPos;
            return false;
        }

        float sqrtD = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrtD) / (2f * a);
        float t2 = (-b - sqrtD) / (2f * a);

        float t = Mathf.Min(t1, t2);
        if (t < 0f)
            t = Mathf.Max(t1, t2);
        if (t <= 0f)
        {
            interceptPoint = targetPos;
            return false;
        }

        interceptPoint = targetPos + targetVelocity * t;
        return true;
    }

    public static bool TryPickChasePoint(Transform agentTransform, Transform targetTransform,
        float flankAngleMax, out Vector3 result)
    {
        Vector3 agentPos = agentTransform.position;
        Vector3 chasePoint = targetTransform.position;

        if (TryGetValidNavMeshPoint(agentPos, chasePoint, DEFAULT_SAMPLE_RADIUS, out result))
            return true;

        if (NavMesh.SamplePosition(chasePoint, out NavMeshHit hit, 5f, AreaMask))
        {
            result = hit.position;
            return true;
        }

        result = agentPos;
        return false;
    }

    public static bool TryPickFleePoint(Vector3 agentPos, Vector3 threatPos,
        float fleeDistance, out Vector3 result)
    {
        Vector3 fleeDir = (agentPos - threatPos);
        fleeDir.y = 0f;

        if (fleeDir.sqrMagnitude < 0.01f)
        {
            fleeDir = Vector3.forward;
        }
        fleeDir.Normalize();

        Vector3 directTarget = agentPos + fleeDir * fleeDistance;
        if (TryGetValidNavMeshPoint(agentPos, directTarget, DEFAULT_SAMPLE_RADIUS, out result))
            return true;

        float[] angleOffsets = { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f };

        foreach (float angleOffset in angleOffsets)
        {
            Vector3 rotatedDir = Quaternion.Euler(0f, angleOffset, 0f) * fleeDir;
            Vector3 candidate = agentPos + rotatedDir * fleeDistance;

            if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
                return true;
        }

        float[] distanceMultipliers = { 0.75f, 0.5f, 0.25f };

        foreach (float multiplier in distanceMultipliers)
        {
            float shorterDistance = fleeDistance * multiplier;

            Vector3 shortDirect = agentPos + fleeDir * shorterDistance;
            if (TryGetValidNavMeshPoint(agentPos, shortDirect, DEFAULT_SAMPLE_RADIUS, out result))
                return true;

            foreach (float angleOffset in angleOffsets)
            {
                Vector3 rotatedDir = Quaternion.Euler(0f, angleOffset, 0f) * fleeDir;
                Vector3 candidate = agentPos + rotatedDir * shorterDistance;

                if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
                    return true;
            }
        }

        if (NavMesh.SamplePosition(agentPos, out NavMeshHit nearHit, fleeDistance, AreaMask))
        {
            Vector3 awayDir = (nearHit.position - threatPos);
            awayDir.y = 0f;
            if (awayDir.sqrMagnitude > 0.1f && Vector3.Dot(awayDir.normalized, fleeDir) > 0f)
            {
                result = nearHit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    public static bool TryPickFleePointAdvanced(Vector3 agentPos, Vector3 threatPos,
        float fleeDistance, float minSafeDistance, out Vector3 result, out bool isCornered)
    {
        isCornered = false;

        if (TryPickFleePoint(agentPos, threatPos, fleeDistance, out result))
        {
            float currentDistSqr = (agentPos - threatPos).sqrMagnitude;
            float newDistSqr = (result - threatPos).sqrMagnitude;

            if (newDistSqr > currentDistSqr)
                return true;
        }

        isCornered = true;

        Vector3 toThreat = (threatPos - agentPos).normalized;
        Vector3 perpendicular = Vector3.Cross(toThreat, Vector3.up).normalized;

        Vector3[] escapeDirections = {
            perpendicular,
            -perpendicular,
            (perpendicular + toThreat * -0.5f).normalized,
            (-perpendicular + toThreat * -0.5f).normalized
        };

        foreach (var escapeDir in escapeDirections)
        {
            Vector3 candidate = agentPos + escapeDir * fleeDistance * 0.5f;
            if (TryGetValidNavMeshPoint(agentPos, candidate, DEFAULT_SAMPLE_RADIUS, out result))
            {
                float newDistSqr = (result - threatPos).sqrMagnitude;
                float minDistSqr = minSafeDistance * minSafeDistance;

                if (newDistSqr >= minDistSqr * 0.5f)
                    return true;
            }
        }

        result = Vector3.zero;
        return false;
    }
}