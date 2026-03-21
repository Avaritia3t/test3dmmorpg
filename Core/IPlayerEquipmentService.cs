public interface IPlayerEquipmentService
{
    /// <summary>Implemented by <see cref="PlayerEquipmentManager"/> (Mirror + ParrelSync prototyping). Server Command wrapper lives in NetworkedPlayerEquipment.cs when enabled.</summary>
    bool EquipItem(Item item);

    /// <summary>Unequip from slot, remove stats via <see cref="IPlayerStatsService"/>.</summary>
    bool UnequipItem(Item item);

    void LogPlayerEquipment();
}
