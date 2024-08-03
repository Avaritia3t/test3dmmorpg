using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupSceneManager : MonoBehaviour
{
    private void Start()
    {
        LoadSetupScene();
    }

    private void LoadSetupScene()
    {
        // Get the current scene name
        string currentScene = SceneManager.GetActiveScene().name;

        // Get the spawn point from the SceneTransitionManager
        Vector3 spawnPoint = SceneTransitionManager.Instance.GetSpawnPointForMap(currentScene);

        // Find the player object and set its position to the spawn point
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            player.transform.position = spawnPoint;
            Debug.Log($"Player moved to spawn point: {spawnPoint}");
        }
        else
        {
            Debug.LogError("Player object not found in the scene.");
        }

        // Additional setup tasks can be added here, like setting up UI, map buffs, etc.
    }
}
