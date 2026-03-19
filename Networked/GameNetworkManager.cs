using UnityEngine;
using Mirror;

/// <summary>
/// Custom Mirror NetworkManager. Server spawns one player per connection via OnServerAddPlayer.
/// Required: one instance in scene (often on same GameObject as NetworkedGameBootstrap). Assign player prefab in inspector.
/// Player prefab must have: NetworkIdentity, SyncPlayerStats, NetworkedDomainController, NavMeshAgent, PlayerStatsManager.
/// Optional: NetworkTransform (Mirror) for movement sync; NetworkedPlayerLootReceiver for loot.
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
