using UnityEngine;

public static class PlayerStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var stats = Object.FindObjectOfType<PlayerStatsManager>();
        if (stats != null)
            locator.Register<IPlayerStatsService>(stats);

        var equipment = Object.FindObjectOfType<PlayerEquipmentManager>();
        if (equipment != null)
            locator.Register<IPlayerEquipmentService>(equipment);
    }
}
