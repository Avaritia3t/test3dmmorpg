using UnityEngine;
using Mirror;

/// <summary>
/// Custom Mirror NetworkManager. Server spawns one player per connection via OnServerAddPlayer.
/// Local testing: ParrelSync clones + Mirror HUD / manual Host vs Client per instance.
/// Required: one instance in scene (often on same GameObject as NetworkedGameBootstrap). Assign player prefab in inspector.
/// Player prefab must have: NetworkIdentity, SyncPlayerStats, NetworkedDomainController, NavMeshAgent, PlayerStatsManager,
/// <see cref="NetworkedPlayerInventory"/>, <see cref="NetworkedPlayerEquipment"/> (server-authoritative equip), <see cref="NetworkedPlayerLootReceiver"/>,
/// <see cref="NetworkedPlayerCombatHelperController"/>, <see cref="NetworkedLocalPlayerServiceRegistrar"/>.
/// Optional: NetworkTransform (Mirror) for movement sync on the player.
/// Optional: NetworkedAbilityExecutor for keyboard ability intents (assign AbilityDefinitionSO assets).
/// </summary>
public class GameNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameNetworkManager] Player prefab is not assigned. Assign it in the NetworkManager inspector.");
            return;
        }

        Vector3 spawnPosition = GetStartPosition();
        if (spawnPosition == Vector3.zero && GameBootstrap.Locator != null)
        {
            var mapService = GameBootstrap.Locator.Get<IMapService>();
            if (mapService?.currentMap != null)
                spawnPosition = mapService.currentMap.spawnPoint;
        }

        GameObject player = spawnPosition != Vector3.zero
            ? Instantiate(playerPrefab, spawnPosition, Quaternion.identity)
            : Instantiate(playerPrefab);

        NetworkServer.AddPlayerForConnection(conn, player);

        // Server: apply map buffs to this player so combat uses correct stats
        var domainController = player.GetComponent<NetworkedDomainController>();
        if (domainController != null)
            domainController.ApplyMapBuffs();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        // Debug.Log($"[GameNetworkManager] Client connected: {conn.connectionId}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        // Debug.Log($"[GameNetworkManager] Client disconnected: {conn.connectionId}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Debug.Log("[GameNetworkManager] Server started.");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // Debug.Log("[GameNetworkManager] Server stopped.");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // Debug.Log("[GameNetworkManager] Connected to server.");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        // Debug.Log("[GameNetworkManager] Disconnected from server.");
    }
}
