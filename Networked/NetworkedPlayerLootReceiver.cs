using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Implements IReceiveLoot so the server can give loot/XP to this connection. XP applied via IPlayerStatsService on same object.
/// Required: on player prefab when using INetworkedLootService. Same GameObject should have PlayerStatsManager (IPlayerStatsService).
/// </summary>
public class NetworkedPlayerLootReceiver : NetworkBehaviour, IReceiveLoot
{
    private NetworkedPlayerInventory inventory;
    private IPlayerStatsService playerStatsService;

    private NetworkedPlayerInventory Inventory => inventory ??= GetComponent<NetworkedPlayerInventory>();
    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GetComponent<IPlayerStatsService>();

    /// <summary>Server-only mirror of bag contents (for debugging / tools). Prefer <see cref="IInventoryService"/> on the same player.</summary>
    public IReadOnlyList<Item> ServerItems => Inventory != null ? Inventory.GetItems() : System.Array.Empty<Item>();

    public IReadOnlyList<Resource> ServerResources => Inventory != null ? Inventory.GetResources() : System.Array.Empty<Resource>();

    public void AddResource(Resource resource)
    {
        if (!NetworkServer.active || resource == null) return;
        Inventory?.ServerAddResource(resource);
    }

    public void AddItem(Item item)
    {
        if (!NetworkServer.active || item == null) return;
        Inventory?.ServerAddItem(item);
    }

    public void AddExperience(int amount)
    {
        if (!NetworkServer.active) return;
        PlayerStatsService?.AddExperience(amount);
    }
}
