using UnityEngine;

public static class CombatStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var combat = Object.FindObjectOfType<CombatHandler>();
        if (combat != null)
            locator.Register<ICombatService>(combat);

        var pool = Object.FindObjectOfType<AttackHandlerPoolV2>();
        if (pool != null)
            locator.Register<IAttackHandlerPool>(pool);

        var networkedPool = Object.FindObjectOfType<NetworkedAttackHandlerPool>();
        if (networkedPool != null)
            locator.Register<INetworkedAttackHandlerPool>(networkedPool);
    }
}
