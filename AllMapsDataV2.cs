using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllMapsDataV2", menuName = "Game/AllMapsDataV2")]
public class AllMapsDataV2 : ScriptableObject
{
    public List<MapDataV2> mapsData;

    private Dictionary<string, MapDataV2> mapsDictionary;

    private void OnEnable()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        if (mapsData == null)
        {
            Debug.LogError("mapsData is null. Ensure it is assigned in the inspector.");
            return;
        }

        mapsDictionary = new Dictionary<string, MapDataV2>();

        foreach (var mapData in mapsData)
        {
            if (mapData == null)
            {
                Debug.LogError("Found a null MapDataV2 in mapsData list. Ensure all elements are properly assigned.");
                continue;
            }

            // Debug.Log($"Adding mapData to dictionary: {mapData.mapID}");
            mapsDictionary[mapData.mapID] = mapData;
        }

        // Debug.Log($"Initialized mapsDictionary with {mapsDictionary.Count} entries.");
    }

    public MapDataV2 GetMapDataByID(string mapID)
    {
        if (mapsDictionary.TryGetValue(mapID, out var mapData))
        {
            Debug.Log($"Found mapData for ID {mapID}: {mapData.mapID}");
            return mapData;
        }
        else
        {
            Debug.LogError($"Map with ID {mapID} not found.");
            return null;
        }
    }
}
