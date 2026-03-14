using UnityEngine;

public class SetupSceneTeleportManager : MonoBehaviour
{
    public float detectionRadius = 100f; // Distance from the edge of the teleporter at which the player can trigger the teleport

    private bool playerInRange = false;
    private Transform player;

    private void Start()
    {
        // Find the player object (assuming it's tagged as "Player")
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        CheckPlayerDistance();

        if (playerInRange && Input.GetKeyDown(KeyCode.T))
        {
            TeleportPlayerToHomeMap();
        }
    }

    private void CheckPlayerDistance()
    {
        if (player != null)
        {
            // Calculate the distance from the edge of the teleporter to the player
            float distanceFromEdge = Vector3.Distance(player.position, transform.position) - GetComponent<SphereCollider>().radius;

            if (distanceFromEdge <= detectionRadius)
            {
                playerInRange = true;
            }
            else
            {
                playerInRange = false;
            }
        }
    }

    private void TeleportPlayerToHomeMap()
    {
        if (player != null)
        {
            // Get the player's faction from the PlayerStatsManager
            string playerFaction = PlayerStatsManager.Instance.playerStats.faction;

            // Use the SceneTransitionManager to load the appropriate scene based on the faction
            SceneTransitionManager.Instance.LoadHomeSceneBasedOnFaction(playerFaction);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the detection radius in the Unity Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetComponent<SphereCollider>().radius + detectionRadius);
    }
}
