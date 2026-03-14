using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupSceneManager : MonoBehaviour
{
    private static ISceneTransitionService _sceneTransitionService;
    private static ISceneTransitionService SceneTransitionService => _sceneTransitionService ??= GameBootstrap.Locator?.Get<ISceneTransitionService>();

    private void Start()
    {
        LoadSetupScene();
    }

    private void LoadSetupScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Vector3 spawnPoint = SceneTransitionService != null ? SceneTransitionService.GetSpawnPointForMap(currentScene) : Vector3.zero;

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
