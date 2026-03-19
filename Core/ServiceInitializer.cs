using UnityEngine;

/// <summary>
/// Runs after all Startup.Configure() calls. Validates that expected services are registered
/// and logs status so you can see which services are up before gameplay.
/// </summary>
public static class ServiceInitializer
{
    /// <summary>
    /// Call once at game start after all Configure(locator) have run.
    /// Logs each required service as OK or missing. Use requireNetworked when the scene
    /// is a Mirror networked game (adds INetworkedAttackHandlerPool, INetworkedLootService).
    /// </summary>
    public static void ValidateAndLogServices(ServiceLocator locator, bool requireNetworked = false)
    {
        if (locator == null)
        {
            Debug.LogError("[ServiceInitializer] Locator is null. Skipping validation.");
            return;
        }

        Debug.Log("[ServiceInitializer] --- Service status ---");

        LogService(locator, "IMapService", () => locator.Get<IMapService>() != null);
        LogService(locator, "IPlayerStatsService", () => locator.Get<IPlayerStatsService>() != null);
        LogService(locator, "IPlayerEquipmentService", () => locator.Get<IPlayerEquipmentService>() != null);
        LogService(locator, "IInventoryService", () => locator.Get<IInventoryService>() != null);
        LogService(locator, "IDropRulesService", () => locator.Get<IDropRulesService>() != null);
        LogService(locator, "ITooltipService", () => locator.Get<ITooltipService>() != null);
        LogService(locator, "ICanvasTransitionService", () => locator.Get<ICanvasTransitionService>() != null);
        LogService(locator, "ISceneTransitionService", () => locator.Get<ISceneTransitionService>() != null);
        LogService(locator, "ICombatService", () => locator.Get<ICombatService>() != null);
        LogService(locator, "IAttackHandlerPool", () => locator.Get<IAttackHandlerPool>() != null);

        if (requireNetworked)
        {
            LogService(locator, "INetworkedAttackHandlerPool", () => locator.Get<INetworkedAttackHandlerPool>() != null);
            LogService(locator, "INetworkedLootService", () => locator.Get<INetworkedLootService>() != null);
        }

        Debug.Log("[ServiceInitializer] --- End service status ---");
    }

    private static void LogService(ServiceLocator locator, string name, System.Func<bool> check)
    {
        bool ok = check();
        if (ok)
            Debug.Log($"[ServiceInitializer] {name}: OK");
        else
            Debug.LogWarning($"[ServiceInitializer] {name}: missing (ensure required scene object and Startup registration).");
    }
}
