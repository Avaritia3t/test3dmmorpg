using System.Collections.Generic;
using System.Reflection;

public interface IPlayerStatsService
{
    PlayerStats playerStats { get; }

    Dictionary<string, PropertyInfo> GetStatProperties();
    bool EquipItem(Item item);
    bool UnequipItem(Item item);
    void SaveStats();
    void LoadStats();
    void AddExperience(int amount);
    void SetPlayerFaction(string faction);
    void SetPlayerClass(string className);
}
