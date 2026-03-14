using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }
    public PlayerStats playerStats;
    private Dictionary<string, PropertyInfo> statProperties;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            playerStats = new PlayerStats();
            InitializeStatProperties();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeStatProperties()
    {
        statProperties = new Dictionary<string, PropertyInfo>();
        var properties = typeof(PlayerStats).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            statProperties[property.Name] = property;
        }
    }

    public Dictionary<string, PropertyInfo> GetStatProperties()
    {
        return statProperties;
    }

    public bool EquipItem(Item item)
    {
        foreach (var slot in playerStats.equipmentSlots)
        {
            if (slot.slotType.ToString() == item.itemType.ToString() && slot.EquipItem(item))
            {
                ApplyItemStats(item);
                item.isEquippable = true;
                SaveStats();
                return true;
            }
        }
        return false;
    }

    public bool UnequipItem(Item item)
    {
        foreach (var slot in playerStats.equipmentSlots)
        {
            if (slot.slotType.ToString() == item.itemType.ToString() && slot.UnequipItem(item))
            {
                RemoveItemStats(item);
                item.isEquippable = false;
                SaveStats();
                return true;
            }
        }
        return false;
    }

    private void ApplyItemStats(Item item)
    {
        foreach (var stat in item.stats)
        {
            ApplyStatModifier(stat);
        }
    }

    private void RemoveItemStats(Item item)
    {
        foreach (var stat in item.stats)
        {
            RemoveStatModifier(stat);
        }
    }

    private void ApplyStatModifier(ItemStat stat)
    {
        if (statProperties.TryGetValue(stat.statName, out var property))
        {
            float currentValue = (float)property.GetValue(playerStats);
            property.SetValue(playerStats, currentValue + stat.statValue);
        }
    }

    private void RemoveStatModifier(ItemStat stat)
    {
        if (statProperties.TryGetValue(stat.statName, out var property))
        {
            float currentValue = (float)property.GetValue(playerStats);
            property.SetValue(playerStats, currentValue - stat.statValue);
        }
    }

    public void SaveStats()
    {
        string json = JsonUtility.ToJson(playerStats);
        PlayerPrefs.SetString("PlayerStats", json);
        PlayerPrefs.Save();
    }

    public void LoadStats()
    {
        if (PlayerPrefs.HasKey("PlayerStats"))
        {
            string json = PlayerPrefs.GetString("PlayerStats");
            playerStats = JsonUtility.FromJson<PlayerStats>(json);
        }
    }

    public void AddExperience(int amount)
    {
        playerStats.experience += amount;
        SaveStats(); // Save stats after modification
    }

    public void SetPlayerFaction(string faction)
    {
        playerStats.faction = faction;
        SaveStats(); // Save the stats after setting the faction
        Debug.Log($"Player faction set to: {faction}");
    }

    public void SetPlayerClass(string className)
    {
        playerStats.className = className;
        SaveStats(); // Save the stats after setting the class
        Debug.Log($"Player class set to: {className}");
    }

}
