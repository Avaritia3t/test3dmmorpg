using UnityEngine;

public static class CombatStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var combat = Object.FindFirstObjectByType<CombatHandler>();
        if (combat != null)
            locator.Register<ICombatService>(combat);

        var pool = Object.FindFirstObjectByType<AttackHandlerPoolV2>();
        if (pool != null)
            locator.Register<IAttackHandlerPool>(pool);

        var networkedPool = Object.FindFirstObjectByType<NetworkedAttackHandlerPool>();
        if (networkedPool != null)
            locator.Register<INetworkedAttackHandlerPool>(networkedPool);
    }
}
