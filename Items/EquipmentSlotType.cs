using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EquipmentSlotType
{
    Weapon,
    Phalanx,
    Artefact,
    Relic,
    Blessing
}

[System.Serializable]
public class EquipmentSlot
{
    /// <summary>Required for JSON persistence of equipment layout.</summary>
    public EquipmentSlot()
    {
        equippedItems = new List<Item>();
    }

    public EquipmentSlotType slotType;
    public int maxSlots;
    public int unlockedSlots;
    public List<Item> equippedItems;

    private static IPlayerStatsService _playerStatsService;
    private static IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();

    public EquipmentSlot(EquipmentSlotType type, int max, int unlocked)
    {
        slotType = type;
        maxSlots = max;
        unlockedSlots = unlocked;
        equippedItems = new List<Item>(maxSlots);
    }

    public bool EquipItem(Item item)
    {
        if (item == null || item.itemType.ToString() != slotType.ToString())
        {
            return false;
        }

        if (equippedItems.Count < unlockedSlots)
        {
            equippedItems.Add(item);
            return true;
        }

        return false;
    }

    public bool UnequipItem(Item item)
    {
        return equippedItems.Remove(item);
    }

    public void CalculateStatsFromEquipment()
    {
        if (PlayerStatsService == null) return;
        var playerStats = PlayerStatsService.playerStats;
        var statProperties = PlayerStatsService.GetStatProperties();
        if (statProperties == null) return;

        // Reset all stats to their base values
        foreach (var property in statProperties.Values)
        {
            property.SetValue(playerStats, 0f); // Assuming base value is 0, adjust if needed
        }

        // Iterate through all equipped items and add their stats
        foreach (var item in equippedItems)
        {
            foreach (var stat in item.stats)
            {
                if (statProperties.TryGetValue(stat.statName, out var property))
                {
                    float currentValue = (float)property.GetValue(playerStats);
                    property.SetValue(playerStats, currentValue + stat.statValue);
                    Debug.Log($"Stat updated: {stat.statName}, New Value: {currentValue + stat.statValue}");
                }
                else
                {
                    Debug.LogWarning($"Stat not found: {stat.statName}");
                }
            }
        }

        // Log final player stats for debugging
        Debug.Log($"Final Player Stats: {GetPlayerStatsSummary()}");
    }

    private string GetPlayerStatsSummary()
    {
        if (PlayerStatsService == null) return "";
        var playerStats = PlayerStatsService.playerStats;
        var statProperties = PlayerStatsService.GetStatProperties();
        if (statProperties == null) return "";
        return string.Join(", ", statProperties.Keys.Select(statName => $"{statName}: {(float)statProperties[statName].GetValue(playerStats)}"));
    }

}
