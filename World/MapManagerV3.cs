using System.Collections.Generic;
using UnityEngine;

public class MapManagerV3 : MonoBehaviour
{
    public static MapManagerV3 Instance { get; private set; }

    [SerializeField]
    private AllMapsDataV2 allMapsData; // Assign in the editor

    public MapDataV2 currentMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Switch to a different map by ID
    public void SwitchMap(string mapID)
    {
        currentMap = allMapsData.GetMapDataByID(mapID);
        if (currentMap != null)
        {
            Debug.Log($"Switched to map: {currentMap.mapID}");

            // Example: Find the player and apply new map buffs
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                ApplyMapBuffs(player);
            }
        }
        else
        {
            Debug.LogError($"Map with ID {mapID} not found.");
        }
    }

    // Apply buffs based on the current map
    public void ApplyMapBuffs(GameObject player)
    {
        if (currentMap == null)
        {
            Debug.LogError("Current map data is null.");
            return;
        }

        DomainControllerV3 playerController = player.GetComponent<DomainControllerV3>();
        if (playerController == null)
        {
            Debug.LogError("Player does not have a DomainControllerV3 component.");
            return;
        }

        string playerFaction = playerController.factionName;
        // Debug.Log($"Applying map buffs. Player Faction: {playerFaction}, Map Faction: {currentMap.factionName}");

        foreach (var buff in currentMap.buffs)
        {
            if ((playerFaction == currentMap.factionName && buff.Type == BuffV2.BuffType.Buff) ||
                (playerFaction != currentMap.factionName && buff.Type == BuffV2.BuffType.Debuff))
            {
                ApplyBuff(playerController, buff);
            }
        }
    }

    private void ApplyBuff(DomainControllerV3 playerController, BuffV2 buff)
    {
        var statProperties = PlayerStatsManager.Instance.GetStatProperties();

        if (statProperties.TryGetValue(buff.StatName, out var property))
        {
            float currentValue = (float)property.GetValue(playerController.playerStatsManager.playerStats);
            property.SetValue(playerController.playerStatsManager.playerStats, currentValue * buff.ModifierValue);

            Debug.Log($"Applied {buff.StatName} {buff.Type} to {playerController.gameObject.name}: {buff.StatName} now {property.GetValue(playerController.playerStatsManager.playerStats)}");
        }
        else
        {
            Debug.LogError($"Stat {buff.StatName} not found on player stats.");
        }
    }
}
