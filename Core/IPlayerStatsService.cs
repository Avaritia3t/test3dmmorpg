using System.Collections.Generic;
using System.Reflection;

public interface IPlayerStatsService
{
    PlayerStats playerStats { get; }

    Dictionary<string, PropertyInfo> GetStatProperties();

    /// <summary>
    /// Apply this item's stat deltas to <see cref="playerStats"/> (reflection). Called by <see cref="IPlayerEquipmentService"/> after a successful slot equip.
    /// Saves and raises <see cref="PlayerStatsManager.StatsChanged"/> for networked combat resync.
    /// </summary>
    void ApplyEquipmentStatModifiers(Item item);

    /// <summary>Reverse <see cref="ApplyEquipmentStatModifiers"/> after unequip.</summary>
    void RemoveEquipmentStatModifiers(Item item);

    void SaveStats();
    void LoadStats();
    void AddExperience(int amount);
    void SetPlayerFaction(string faction);
    void SetPlayerClass(string className);
}
