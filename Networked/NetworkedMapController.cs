using System.Collections.Generic;
using UnityEngine;

public class NetworkedMapController : MonoBehaviour, IMapService
{
    [SerializeField]
    private AllMapsDataV2 allMapsData; // Assign in the editor

    public MapDataV2 currentMap;

    private IPlayerStatsService _playerStatsService;
    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
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

        NetworkedDomainController playerController = player.GetComponent<NetworkedDomainController>();
        if (playerController == null)
        {
            Debug.LogError("Player does not have a NetworkedDomainController component.");
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

    private void ApplyBuff(NetworkedDomainController playerController, BuffV2 buff)
    {
        if (PlayerStatsService == null) return;
        var statProperties = PlayerStatsService.GetStatProperties();

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
