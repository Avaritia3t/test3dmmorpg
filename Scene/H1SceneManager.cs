using UnityEngine;
using UnityEngine.AI;

public class H1SceneManager : MonoBehaviour
{
    private IMapService _mapService;
    private IAttackHandlerPool _attackHandlerPool;
    private IMapService MapService => _mapService ??= GameBootstrap.Locator?.Get<IMapService>();
    private IAttackHandlerPool AttackHandlerPool => _attackHandlerPool ??= GameBootstrap.Locator?.Get<IAttackHandlerPool>();

    void Start()
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        Debug.Log("Initializing H1 Scene...");

        if (MapService == null)
        {
            Debug.LogError("IMapService not found.");
            return;
        }
        MapService.SwitchMap("H1");

        MapDataV2 mapData = MapService.currentMap;
        if (mapData != null)
        {
            Debug.Log($"H1 map data loaded: {mapData.mapID}");
            SetPlayerSpawnPoint(mapData.spawnPoint);
        }
        else
        {
            Debug.LogError("H1 map data could not be loaded.");
        }

        if (AttackHandlerPool != null)
        {
            AttackHandlerPool.InitializePool();
            Debug.Log("AttackHandlerPool initialized for H1 Scene.");
        }
        else
        {
            Debug.LogError("IAttackHandlerPool not found.");
        }
    }

    private void SetPlayerSpawnPoint(Vector3 spawnPoint)
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var player = playerObj;
            NavMeshAgent agent = playerObj.GetComponent<NavMeshAgent>();

            // Attempt to find the NavMesh Y position at the given X and Z coordinates
            Vector3 navMeshPoint = new Vector3(spawnPoint.x, spawnPoint.y + 200f, spawnPoint.z); // Start the raycast from above the spawn point
            NavMeshHit hit;
            float maxDistance = 200f; // Maximum distance to check below the spawn point

            // Perform a downward raycast to find the NavMesh
            if (NavMesh.SamplePosition(navMeshPoint, out hit, maxDistance, NavMesh.AllAreas))
            {
                // Use the Y value from the NavMesh hit position to adjust the spawn point
                Vector3 adjustedSpawnPoint = new Vector3(spawnPoint.x, hit.position.y, spawnPoint.z);
                agent.Warp(adjustedSpawnPoint); // Warp the player to the adjusted position
                Debug.Log($"Player spawn point set to: {adjustedSpawnPoint.ToString("F2")} on NavMesh.");
            }
            else
            {
                Debug.LogError($"Could not find a valid NavMesh position within {maxDistance} units below the intended spawn point ({spawnPoint}).");
            }

            // Log the player's final position
            Debug.Log($"Player current position after warp: {player.transform.position.ToString("F2")}");
        }
        else
        {
            Debug.LogError("Player object not found.");
        }
    }
}
