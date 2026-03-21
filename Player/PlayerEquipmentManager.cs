using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Single equip pipeline: update equipment slots on <see cref="IPlayerStatsService.playerStats"/>,
/// then apply/remove stat deltas through <see cref="IPlayerStatsService.ApplyEquipmentStatModifiers"/> /
/// <see cref="IPlayerStatsService.RemoveEquipmentStatModifiers"/> (implemented by <see cref="PlayerStatsManager"/>).
/// <see cref="EquipmentStats"/> tracks totals from equipment only (UI/debug). All numeric player changes go through stats service.
/// <para>
/// Optional: server Command wrapper in NetworkedPlayerEquipment.cs when that file is compiled (#if true).
/// </para>
/// </summary>
public class PlayerEquipmentManager : MonoBehaviour, IPlayerEquipmentService
{
    public List<Item> equippedItems = new List<Item>();
    public PlayerStats EquipmentStats { get; private set; }

    private Dictionary<string, PropertyInfo> statProperties;
    private IPlayerStatsService _playerStatsService;

    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GetComponent<IPlayerStatsService>();

    private void Awake()
    {
        EquipmentStats = new PlayerStats();
        DontDestroyOnLoad(gameObject);
        _playerStatsService = GetComponent<IPlayerStatsService>();
    }

    private void Start()
    {
        EquipmentStats = new PlayerStats();
        ResetEquipmentStats();
        statProperties = PlayerStatsService?.GetStatProperties();
    }

    public bool EquipItem(Item item)
    {
        if (item == null || PlayerStatsService == null)
            return false;

        foreach (var slot in PlayerStatsService.playerStats.equipmentSlots)
        {
            if (slot.slotType.ToString() != item.itemType.ToString())
                continue;
            if (!slot.EquipItem(item))
                continue;

            PlayerStatsService.ApplyEquipmentStatModifiers(item);
            AddEquipmentStatsOnly(item);
            if (!equippedItems.Contains(item))
                equippedItems.Add(item);
            return true;
        }

        return false;
    }

    public bool UnequipItem(Item item)
    {
        if (item == null || PlayerStatsService == null)
            return false;

        foreach (var slot in PlayerStatsService.playerStats.equipmentSlots)
        {
            if (slot.slotType.ToString() != item.itemType.ToString())
                continue;
            if (!slot.UnequipItem(item))
                continue;

            PlayerStatsService.RemoveEquipmentStatModifiers(item);
            RemoveEquipmentStatsOnly(item);
            equippedItems.Remove(item);
            return true;
        }

        return false;
    }

    /// <summary>Server: find an equipped item matching identity fields (for Command unequip).</summary>
    public Item FindEquippedItemMatching(string itemName, ItemType itemType, string subtype, int level)
    {
        if (PlayerStatsService == null)
            return null;
        string st = subtype ?? "";
        foreach (var slot in PlayerStatsService.playerStats.equipmentSlots)
        {
            foreach (var it in slot.equippedItems)
            {
                if (it == null)
                    continue;
                if (it.itemName == itemName && it.itemType == itemType && it.level == level && (it.subtype ?? "") == st)
                    return it;
            }
        }
        return null;
    }

    private void ResetEquipmentStats()
    {
        if (PlayerStatsService == null)
            return;
        statProperties = PlayerStatsService.GetStatProperties();
        if (statProperties == null)
            return;

        foreach (var property in statProperties.Values)
        {
            if (property.PropertyType == typeof(float))
                property.SetValue(EquipmentStats, 0f);
        }
    }

    private void AddEquipmentStatsOnly(Item item)
    {
        if (item?.stats == null || statProperties == null)
            return;
        foreach (var stat in item.stats)
            AddEquipmentStatOnly(stat);
    }

    private void AddEquipmentStatOnly(ItemStat stat)
    {
        if (statProperties == null || !statProperties.TryGetValue(stat.statName, out var property))
        {
            Debug.LogWarning($"[PlayerEquipmentManager] Stat not found: {stat.statName}");
            return;
        }

        float currentEquipmentValue = (float)property.GetValue(EquipmentStats);
        property.SetValue(EquipmentStats, currentEquipmentValue + stat.statValue);
    }

    private void RemoveEquipmentStatsOnly(Item item)
    {
        if (item?.stats == null || statProperties == null)
            return;
        foreach (var stat in item.stats)
            RemoveEquipmentStatOnly(stat);
    }

    private void RemoveEquipmentStatOnly(ItemStat stat)
    {
        if (statProperties == null || !statProperties.TryGetValue(stat.statName, out var property))
        {
            Debug.LogWarning($"[PlayerEquipmentManager] Stat not found: {stat.statName}");
            return;
        }

        float currentEquipmentValue = (float)property.GetValue(EquipmentStats);
        property.SetValue(EquipmentStats, currentEquipmentValue - stat.statValue);
    }

    public void LogPlayerEquipment()
    {
        Debug.Log("Logging Player Equipment:");

        if (PlayerStatsService == null)
            return;
        var equipmentSlots = PlayerStatsService.playerStats.equipmentSlots;

        foreach (var slot in equipmentSlots)
        {
            Debug.Log($"Slot Type: {slot.slotType}");
            foreach (var equippedItem in slot.equippedItems)
            {
                Debug.Log($" - Equipped Item: {equippedItem.itemName}, Type: {equippedItem.itemType}, Rarity: {equippedItem.itemRarity}, Level: {equippedItem.level}");
                foreach (var stat in equippedItem.stats)
                    Debug.Log($" -- Stat: {stat.statName}, Value: {stat.statValue}");
            }
        }
    }
}
