using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Implements IReceiveLoot so the server can give loot/XP to this connection. XP applied via IPlayerStatsService on same object.
/// Required: on player prefab when using INetworkedLootService. Same GameObject should have PlayerStatsManager (IPlayerStatsService).
/// </summary>
public class NetworkedPlayerLootReceiver : NetworkBehaviour, IReceiveLoot
{
    private readonly List<Item> serverItems = new List<Item>();
    private readonly List<Resource> serverResources = new List<Resource>();

    private IPlayerStatsService playerStatsService;

    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GetComponent<IPlayerStatsService>();

    public IReadOnlyList<Item> ServerItems => serverItems;
    public IReadOnlyList<Resource> ServerResources => serverResources;

    public void AddResource(Resource resource)
    {
        if (!NetworkServer.active || resource == null) return;
        serverResources.Add(resource);
    }

    public void AddItem(Item item)
    {
        if (!NetworkServer.active || item == null) return;
        serverItems.Add(item);
    }

    public void AddExperience(int amount)
    {
        if (!NetworkServer.active) return;
        PlayerStatsService?.AddExperience(amount);
    }
}
