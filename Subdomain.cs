using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SubdomainType
{
    Volcano, Mountain, Forest, Foothill, Plain, Industrial, Valley, Coastline, Aquatic, Seafloor, Island
}


[System.Serializable]
public class Subdomain
{
    public string name;
    public int level;
    public SubdomainType type;
    public float depletionInterval; // Time in seconds since last occupied
    public float regenerationSpeed;
    public string factionTerritory;
    public string territoryModifier; // Consider making this an enum or struct if there are predefined modifiers

    public List<NPC> npcs;
    public List<Resource> resources;

    // Use this to track the last occupied time
    private float lastOccupiedTime;

    public Subdomain(string name, int level, SubdomainType type, float regenerationSpeed, string factionTerritory, string territoryModifier)
    {
        this.name = name;
        this.level = level;
        this.type = type;
        this.regenerationSpeed = regenerationSpeed;
        this.factionTerritory = factionTerritory;
        this.territoryModifier = territoryModifier;

        npcs = new List<NPC>();
        resources = new List<Resource>();

        // Initialize depletion interval and last occupied time
        this.depletionInterval = 0f;
        this.lastOccupiedTime = Time.time;
    }

    // Call this method to update the subdomain status; could be called each frame or at intervals
    public void UpdateSubdomainStatus()
    {
        // Update depletion interval based on current time and last occupied time
        depletionInterval = Time.time - lastOccupiedTime;

        // Example: Regenerate resources based on depletionInterval and regenerationSpeed
        foreach (var resource in resources)
        {
            // Assuming each resource has a method to regenerate based on time
            // This is a simplified example; actual regeneration might depend on specific rules
            resource.quantity += Mathf.FloorToInt(depletionInterval * regenerationSpeed * resource.regenerationSpeed);
        }

        // Reset depletion interval and last occupied time after regeneration
        lastOccupiedTime = Time.time;
        depletionInterval = 0f;
    }

    // Call this when a player occupies the subdomain to reset the timer
    public void OccupySubdomain()
    {
        lastOccupiedTime = Time.time;
    }
}
