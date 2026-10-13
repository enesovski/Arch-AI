using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpawnZoneKeeper : MonoBehaviour
{
    public List<Bounds> spawnZones;
    public List<Bounds> safeZones;

    public void AddPointOfInterest(Bounds bounds)
    {
        spawnZones.Add(bounds);
    }

    public void RemovePointOfInterest(Bounds bounds)
    {
        spawnZones.Remove(bounds);
    }

    public void AddSafeZone(Bounds bounds)
    {
        safeZones.Add(bounds);
    }

    public void RemoveSafeZone(Bounds bounds)
    {
        safeZones.Remove(bounds);
    }

    public void UpdateZone(Bounds oldBounds, Bounds newBounds)
    {
        // This could be for either POIs or Safe Zones, depending on your game logic
        if (spawnZones.Contains(oldBounds))
        {
            spawnZones.Remove(oldBounds);
            spawnZones.Add(newBounds);
        }
        else if (safeZones.Contains(oldBounds))
        {
            safeZones.Remove(oldBounds);
            safeZones.Add(newBounds);
        }
    }
}
