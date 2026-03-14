public interface IPlayerEquipmentService
{
    void EquipItem(Item item);
    void UnequipItem(Item item);
    void CalculateStatsAdditionFromEquipment(Item item);
    void CalculateStatsReductionFromEquipment(Item item);
}
