using UnityEngine;
using UnityEngine.AI;

public class H1SceneManager : MonoBehaviour
{
    void Start()
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        Debug.Log("Initializing H1 Scene...");

        // Switch to the N1 map
        MapManagerV3.Instance.SwitchMap("H1");

        // Ensure the map data is correctly loaded
        MapDataV2 mapData = MapManagerV3.Instance.currentMap;
        if (mapData != null)
        {
            Debug.Log($"H1 map data loaded: {mapData.mapID}");
            SetPlayerSpawnPoint(mapData.spawnPoint);
        }
        else
        {
            Debug.LogError("H1 map data could not be loaded.");
        }

        // Ensure the AttackHandler pool is initialized
        if (AttackHandlerPoolV2.Instance != null)
        {
            AttackHandlerPoolV2.Instance.InitializePool();
            Debug.Log("AttackHandlerPool initialized for H1 Scene.");
        }
        else
        {
            Debug.LogError("AttackHandlerPoolV2 instance not found.");
        }
    }

    private void SetPlayerSpawnPoint(Vector3 spawnPoint)
    {
        if (DomainControllerV3.Instance != null)
        {
            var player = DomainControllerV3.Instance.gameObject;
            NavMeshAgent agent = DomainControllerV3.Instance.GetComponent<NavMeshAgent>();

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
