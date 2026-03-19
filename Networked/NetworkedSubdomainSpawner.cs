using UnityEngine;
using Mirror;

/// <summary>
/// Server-only: spawn subdomain prefabs and register with Mirror. Call from TerrainUtils or other map/terrain code.
/// No component on scene: use static ServerSpawnSubdomain(...). Prefab must have: NetworkIdentity, SyncSubdomainState, NetworkedSubdomainController.
/// </summary>
public static class NetworkedSubdomainSpawner
{
    /// <summary>
    /// Server only. Instantiates the prefab at position, applies server-authoritative stats, spawns so clients see it.
    /// </summary>
    public static GameObject ServerSpawnSubdomain(GameObject prefab, Vector3 position, Quaternion rotation, int level, SubdomainV2Type type)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[NetworkedSubdomainSpawner] ServerSpawnSubdomain called but not server.");
            return null;
        }
        if (prefab == null) return null;

        GameObject go = Object.Instantiate(prefab, position, rotation);
        var controller = go.GetComponent<NetworkedSubdomainController>();
        if (controller != null)
            controller.ApplySubdomainStatsFromServer(level, type);
        NetworkServer.Spawn(go);
        return go;
    }

    /// <summary>Random subdomain type for server-side placement (matches NetworkedSubdomainController distribution).</summary>
    public static SubdomainV2Type GetRandomSubdomainType()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < 30) return SubdomainV2Type.Badlands;
        if (randomValue < 40) return SubdomainV2Type.Hovel;
        if (randomValue < 50) return SubdomainV2Type.Hearthstead;
        if (randomValue < 60) return SubdomainV2Type.Thorp;
        if (randomValue < 70) return SubdomainV2Type.Borough;
        if (randomValue < 80) return SubdomainV2Type.Civicron;
        if (randomValue < 85) return SubdomainV2Type.Arcanopolis;
        if (randomValue < 90) return SubdomainV2Type.Dominionhold;
        if (randomValue < 95) return SubdomainV2Type.Sovereignty;
        return SubdomainV2Type.Apex;
    }
}
