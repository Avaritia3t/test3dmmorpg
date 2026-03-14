using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class PlayerEquipmentManager : MonoBehaviour, IPlayerEquipmentService
{
    public List<Item> equippedItems = new List<Item>();
    public PlayerStats EquipmentStats { get; private set; }
    private Dictionary<string, PropertyInfo> statProperties;

    private IPlayerStatsService _playerStatsService;
    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();

    private void Awake()
    {
        EquipmentStats = new PlayerStats();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EquipmentStats = new PlayerStats();
        ResetEquipmentStats(); // Ensure all stats are set to zero
        statProperties = PlayerStatsService?.GetStatProperties();
    }

    public void EquipItem(Item item)
    {
        Debug.Log($"Equipping item: {item.itemName}");

        if (PlayerStatsService == null) return;
        foreach (var slot in PlayerStatsService.playerStats.equipmentSlots)
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

        if (PlayerStatsService == null) return;
        foreach (var slot in PlayerStatsService.playerStats.equipmentSlots)
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
        if (PlayerStatsService == null) return;
        statProperties = PlayerStatsService.GetStatProperties();
        if (statProperties == null) return;

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
        if (PlayerStatsService == null || statProperties == null) return;
        if (statProperties.TryGetValue(stat.statName, out var property))
        {
            float currentEquipmentValue = (float)property.GetValue(EquipmentStats);
            float currentPlayerValue = (float)property.GetValue(PlayerStatsService.playerStats);
            property.SetValue(EquipmentStats, currentEquipmentValue + stat.statValue);
            property.SetValue(PlayerStatsService.playerStats, currentPlayerValue + stat.statValue);
            Debug.Log($"Equipment stat updated: {stat.statName}, New Equipment Value: {currentEquipmentValue + stat.statValue}, New Player Value: {currentPlayerValue + stat.statValue}");
        }
        else
        {
            Debug.LogWarning($"Stat not found: {stat.statName}");
        }
    }

    private void RemoveStatFromEquipmentStats(ItemStat stat)
    {
        if (PlayerStatsService == null || statProperties == null) return;
        if (statProperties.TryGetValue(stat.statName, out var property))
        {
            float currentEquipmentValue = (float)property.GetValue(EquipmentStats);
            float currentPlayerValue = (float)property.GetValue(PlayerStatsService.playerStats);
            float newEquipmentValue = currentEquipmentValue - stat.statValue;
            float newPlayerValue = currentPlayerValue - stat.statValue;
            property.SetValue(EquipmentStats, newEquipmentValue);
            property.SetValue(PlayerStatsService.playerStats, newPlayerValue);
            Debug.Log($"Removing stat: {stat.statName}, New Equipment Value: {newEquipmentValue}, New Player Value: {newPlayerValue}");
        }
        else
        {
            Debug.LogWarning($"Stat not found: {stat.statName}");
        }
    }

    private string GetPlayerStatsSummary()
    {
        if (PlayerStatsService == null) return "";
        var stats = PlayerStatsService.playerStats;
        var props = PlayerStatsService.GetStatProperties();
        if (props == null) return "";
        return string.Join(", ", props.Keys.Select(statName => $"{statName}: {(float)props[statName].GetValue(stats)}"));
    }

    public void LogPlayerEquipment()
    {
        Debug.Log("Logging Player Equipment:");

        if (PlayerStatsService == null) return;
        var equipmentSlots = PlayerStatsService.playerStats.equipmentSlots;

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
