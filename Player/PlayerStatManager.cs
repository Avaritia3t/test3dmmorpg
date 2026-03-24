using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Authoritative <see cref="PlayerStats"/> for this player instance (one per player prefab).
/// <para><b>Mirror:</b> Combat (attack interval, damage rolls, crits) runs on the <b>server</b> and reads this component on the
/// server-owned player object. Routes that mutate stats must run on that same server instance:</para>
/// <list type="bullet">
/// <item><b>Spawn / map:</b> <see cref="GameNetworkManager.OnServerAddPlayer"/> → <see cref="NetworkedDomainController.ApplyMapBuffs"/> → <see cref="NetworkedMapController.ApplyMapBuffs"/> (server).</item>
/// <item><b>Loot XP:</b> <see cref="NetworkedPlayerLootReceiver.AddExperience"/> (server only).</item>
/// <item><b>Equipment:</b> <see cref="NetworkedPlayerEquipment"/> (Commands) → <see cref="PlayerEquipmentManager"/> / <see cref="IPlayerStatsService.ApplyEquipmentStatModifiers"/> on the server.</item>
/// <item><b>Damage / regen:</b> <see cref="NetworkedDomainController.TakeDamage"/> and regen coroutine (server).</item>
/// </list>
/// </summary>
public class PlayerStatsManager : MonoBehaviour, IPlayerStatsService
{
    public PlayerStats playerStats;
    private Dictionary<string, PropertyInfo> statProperties;

    /// <summary>
    /// Raised when stats change (equipment modifiers, load, etc.). Networked combat resyncs from this.
    /// </summary>
    public event System.Action StatsChanged;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        playerStats = new PlayerStats();
        InitializeStatProperties();
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

    /// <inheritdoc />
    public void ApplyEquipmentStatModifiers(Item item)
    {
        if (item?.stats == null)
            return;
        foreach (var stat in item.stats)
            ApplyStatModifier(stat);
        item.isEquippable = true;
        SaveStats();
        StatsChanged?.Invoke();
    }

    /// <inheritdoc />
    public void RemoveEquipmentStatModifiers(Item item)
    {
        if (item?.stats == null)
            return;
        foreach (var stat in item.stats)
            RemoveStatModifier(stat);
        item.isEquippable = false;
        SaveStats();
        StatsChanged?.Invoke();
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
            StatsChanged?.Invoke();
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
