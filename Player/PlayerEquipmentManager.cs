using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class PlayerEquipmentManager : MonoBehaviour
{
    public static PlayerEquipmentManager Instance { get; private set; }
    public List<Item> equippedItems = new List<Item>();
    public PlayerStats EquipmentStats { get; private set; } // New variable for equipment stats
    private Dictionary<string, PropertyInfo> statProperties;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EquipmentStats = new PlayerStats();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EquipmentStats = new PlayerStats();
        ResetEquipmentStats(); // Ensure all stats are set to zero
        statProperties = PlayerStatsManager.Instance.GetStatProperties();
    }

    public void EquipItem(Item item)
    {
        Debug.Log($"Equipping item: {item.itemName}");

        foreach (var slot in PlayerStatsManager.Instance.playerStats.equipmentSlots)
        {
            if (slot.slotType.ToString() == item.itemType.ToString() && slot.EquipItem(item))
            {
                return;
            }
        }
    }

    public void UnequipItem(Item item)
    {
        Debug.Log($"Unequipping item: {item.itemName}");

        foreach (var slot in PlayerStatsManager.Instance.playerStats.equipmentSlots)
        {
            if (slot.slotType.ToString() == item.itemType.ToString() && slot.UnequipItem(item))
            {
                return;
            }
        }
    }

    public void CalculateStatsAdditionFromEquipment(Item item)
    {
        // Add stats from the specified item
        foreach (var stat in item.stats)
        {
            AddStatToEquipmentStats(stat);
        }
    }

    public void CalculateStatsReductionFromEquipment(Item item)
    {
        // Subtract stats from the specified item
        foreach (var stat in item.stats)
        {
            RemoveStatFromEquipmentStats(stat);
        }
    }

    private void ResetEquipmentStats()
    {
        var statProperties = PlayerStatsManager.Instance.GetStatProperties();

        foreach (var property in statProperties.Values)
        {
            if (property.PropertyType == typeof(float))
            {
                property.SetValue(EquipmentStats, 0f);
            }
        }
    }

    private void AddStatToEquipmentStats(ItemStat stat)
    {
        if (statProperties.TryGetValue(stat.statName, out var property))
        {
            float currentEquipmentValue = (float)property.GetValue(EquipmentStats);
            float currentPlayerValue = (float)property.GetValue(PlayerStatsManager.Instance.playerStats);
            property.SetValue(EquipmentStats, currentEquipmentValue + stat.statValue);
            property.SetValue(PlayerStatsManager.Instance.playerStats, currentPlayerValue + stat.statValue);
            Debug.Log($"Equipment stat updated: {stat.statName}, New Equipment Value: {currentEquipmentValue + stat.statValue}, New Player Value: {currentPlayerValue + stat.statValue}");
        }
        else
        {
            Debug.LogWarning($"Stat not found: {stat.statName}");
        }
    }

    private void RemoveStatFromEquipmentStats(ItemStat stat)
    {
        if (statProperties.TryGetValue(stat.statName, out var property))
        {
            float currentEquipmentValue = (float)property.GetValue(EquipmentStats);
            float currentPlayerValue = (float)property.GetValue(PlayerStatsManager.Instance.playerStats);
            float newEquipmentValue = currentEquipmentValue - stat.statValue;
            float newPlayerValue = currentPlayerValue - stat.statValue;
            property.SetValue(EquipmentStats, newEquipmentValue);
            property.SetValue(PlayerStatsManager.Instance.playerStats, newPlayerValue);
            Debug.Log($"Removing stat: {stat.statName}, New Equipment Value: {newEquipmentValue}, New Player Value: {newPlayerValue}");
        }
        else
        {
            Debug.LogWarning($"Stat not found: {stat.statName}");
        }
    }

    private string GetPlayerStatsSummary()
    {
        var playerStats = PlayerStatsManager.Instance.playerStats;
        var statProperties = PlayerStatsManager.Instance.GetStatProperties();
        return string.Join(", ", statProperties.Keys.Select(statName => $"{statName}: {(float)statProperties[statName].GetValue(playerStats)}"));
    }

    public void LogPlayerEquipment()
    {
        Debug.Log("Logging Player Equipment:");

        var equipmentSlots = PlayerStatsManager.Instance.playerStats.equipmentSlots;

        foreach (var slot in equipmentSlots)
        {
            Debug.Log($"Slot Type: {slot.slotType}");
            foreach (var equippedItem in slot.equippedItems)
            {
                Debug.Log($" - Equipped Item: {equippedItem.itemName}, Type: {equippedItem.itemType}, Rarity: {equippedItem.itemRarity}, Level: {equippedItem.level}");
                foreach (var stat in equippedItem.stats)
                {
                    Debug.Log($" -- Stat: {stat.statName}, Value: {stat.statValue}");
                }
            }
        }
    }
}
