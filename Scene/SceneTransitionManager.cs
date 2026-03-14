using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour, ISceneTransitionService
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadHomeSceneBasedOnFaction(string faction)
    {
        string sceneName = DetermineHomeSceneName(faction);
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Failed to load scene: Scene name is empty or null.");
        }
    }

    private string DetermineHomeSceneName(string faction)
    {
        switch (faction)
        {
            case "Heritage":
                return "H1";
            case "Publicus":
                return "P1";
            case "Nadiria":
                return "N1";
            case "Forsaken":
                return "F1";
            case "Lapsu":
                return "L1";
            default:
                Debug.LogWarning("Faction not recognized: " + faction);
                return null;
        }
    }

    public Vector3 GetSpawnPointForMap(string sceneName)
    {
        MapDataV2 mapData = LoadMapData(sceneName);
        if (mapData != null)
        {
            return mapData.spawnPoint;
        }
        else
        {
            Debug.LogError("Map data not found for scene: " + sceneName);
            return Vector3.zero; // Default fallback position
        }
    }

    private MapDataV2 LoadMapData(string sceneName)
    {
        // Load the MapDataV2 object based on the scene name
        string mapDataName = sceneName + "MapData";
        MapDataV2 mapData = Resources.Load<MapDataV2>("MapDataFiles/" + mapDataName);

        if (mapData == null)
        {
            Debug.LogError("MapDataV2 not found for: " + mapDataName);
        }

        return mapData;
    }
}
