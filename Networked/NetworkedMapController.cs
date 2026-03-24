using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IMapService for networked games. SwitchMap sets currentMap; each player applies buffs to themselves in Awake.
/// Required: one instance in scene (DontDestroyOnLoad). Assign allMapsData. WorldStartup registers as IMapService when present.
/// </summary>
public class NetworkedMapController : MonoBehaviour, IMapService
{
    [SerializeField]
    private AllMapsDataV2 allMapsData; // Assign in the editor

    public MapDataV2 currentMap { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Switch to a different map by ID. Only sets currentMap; each player applies buffs to themselves in NetworkedDomainController.Awake.
    public void SwitchMap(string mapID)
    {
        currentMap = allMapsData.GetMapDataByID(mapID);
        if (currentMap != null)
            Debug.Log($"Switched to map: {currentMap.mapID}");
        else
            Debug.LogError($"Map with ID {mapID} not found.");
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
        // Must use the spawned player's PlayerStatsManager — not GameBootstrap.Locator (host-only / wrong on dedicated server).
        var psm = playerController.playerStatsManager;
        if (psm == null) return;
        var statProperties = psm.GetStatProperties();

        if (statProperties.TryGetValue(buff.StatName, out var property))
        {
            float currentValue = (float)property.GetValue(psm.playerStats);
            property.SetValue(psm.playerStats, currentValue * buff.ModifierValue);

            Debug.Log($"Applied {buff.StatName} {buff.Type} to {playerController.gameObject.name}: {buff.StatName} now {property.GetValue(psm.playerStats)}");
        }
        else
        {
            Debug.LogError($"Stat {buff.StatName} not found on player stats.");
        }
    }
}
