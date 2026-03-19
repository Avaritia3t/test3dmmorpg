using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// Scene setup for N1 (networked): SwitchMap, spawn point for all connections, optional server init of INetworkedAttackHandlerPool.
/// Required: one instance in N1 scene. No component requirements; uses MapService and optional INetworkedAttackHandlerPool from locator.
/// </summary>
[DefaultExecutionOrder(-500)]
public class NetworkedN1SceneController : MonoBehaviour
{
    private IMapService mapService;
    private INetworkedAttackHandlerPool networkedAttackHandlerPool;
    private IMapService MapService => mapService ??= GameBootstrap.Locator?.Get<IMapService>();
    private INetworkedAttackHandlerPool AttackHandlerPool => networkedAttackHandlerPool ??= GameBootstrap.Locator?.Get<INetworkedAttackHandlerPool>();

    void Start()
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        Debug.Log("Initializing N1 Scene (Networked)...");

        if (MapService == null)
        {
            Debug.LogError("IMapService not found.");
            return;
        }
        MapService.SwitchMap("N1");

        MapDataV2 mapData = MapService.currentMap;
        if (mapData != null)
        {
            Debug.Log($"N1 map data loaded: {mapData.mapID}");
            SetPlayerSpawnPoint(mapData.spawnPoint);
        }
        else
        {
            Debug.LogError("N1 map data could not be loaded.");
        }

        if (NetworkServer.active && AttackHandlerPool != null)
        {
            AttackHandlerPool.InitializePool();
            Debug.Log("NetworkedAttackHandlerPool initialized for N1 Scene (server).");
        }
        else if (AttackHandlerPool == null)
        {
            Debug.LogError("INetworkedAttackHandlerPool not found.");
        }
    }

    private void SetPlayerSpawnPoint(Vector3 spawnPoint)
    {
        Vector3 adjustedSpawnPoint;
        if (!TryGetAdjustedSpawnOnNavMesh(spawnPoint, out adjustedSpawnPoint))
        {
            Debug.LogError($"Could not find a valid NavMesh position below the intended spawn point ({spawnPoint}).");
            return;
        }

        if (NetworkServer.active)
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn?.identity == null) continue;
                WarpPlayerTo(conn.identity.gameObject, adjustedSpawnPoint);
            }
        }
        else
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                WarpPlayerTo(playerObj, adjustedSpawnPoint);
            else
                Debug.LogError("Player object not found.");
        }
    }

    private static bool TryGetAdjustedSpawnOnNavMesh(Vector3 spawnPoint, out Vector3 adjusted)
    {
        adjusted = spawnPoint;
        Vector3 navMeshPoint = new Vector3(spawnPoint.x, spawnPoint.y + 200f, spawnPoint.z);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(navMeshPoint, out hit, 200f, NavMesh.AllAreas))
        {
            adjusted = new Vector3(spawnPoint.x, hit.position.y, spawnPoint.z);
            return true;
        }
        return false;
    }

    private static void WarpPlayerTo(GameObject playerObj, Vector3 position)
    {
        var agent = playerObj.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.Warp(position);
        else
            playerObj.transform.position = position;
        Debug.Log($"Player {playerObj.name} spawn point set to: {position.ToString("F2")}.");
    }
}
