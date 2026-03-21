using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Server-side: routes loot/XP to the connection's player via IReceiveLoot. Registers as INetworkedLootService in Awake.
/// Required: one instance in scene when using Mirror. Player prefab must have NetworkedPlayerLootReceiver (IReceiveLoot).
/// </summary>
public class NetworkedLootService : MonoBehaviour, INetworkedLootService
{
    private void Awake()
    {
        if (GameBootstrap.Locator != null)
            GameBootstrap.Locator.Register<INetworkedLootService>(this);
    }

    private static IReceiveLoot GetReceiverForConnection(NetworkConnectionToClient conn)
    {
        if (conn?.identity == null) return null;
        return conn.identity.GetComponent<IReceiveLoot>();
    }

    public void AddResourceForConnection(NetworkConnectionToClient conn, Resource resource)
    {
        if (!NetworkServer.active || resource == null) return;
        GetReceiverForConnection(conn)?.AddResource(resource);
    }

    public void AddItemForConnection(NetworkConnectionToClient conn, Item item)
    {
        if (!NetworkServer.active || item == null) return;
        GetReceiverForConnection(conn)?.AddItem(item);
    }

    public void AddExperienceForConnection(NetworkConnectionToClient conn, int amount)
    {
        if (!NetworkServer.active) return;
        GetReceiverForConnection(conn)?.AddExperience(amount);
    }
}
