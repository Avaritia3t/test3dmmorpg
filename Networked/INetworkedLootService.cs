using Mirror;

/// <summary>
/// Server-side: route loot/XP to a connection's player via IReceiveLoot. Implemented by NetworkedLootService.
/// Required: one NetworkedLootService in scene (registers in Awake). Player prefab needs IReceiveLoot (e.g. NetworkedPlayerLootReceiver).
/// </summary>
public interface INetworkedLootService
{
    void AddResourceForConnection(NetworkConnectionToClient conn, Resource resource);
    void AddItemForConnection(NetworkConnectionToClient conn, Item item);
    void AddExperienceForConnection(NetworkConnectionToClient conn, int amount);
}

/// <summary>
/// Implement on the player (e.g. NetworkedPlayerLootReceiver). Server calls this when giving loot/XP to that connection.
/// </summary>
public interface IReceiveLoot
{
    void AddResource(Resource resource);
    void AddItem(Item item);
    void AddExperience(int amount);
}
