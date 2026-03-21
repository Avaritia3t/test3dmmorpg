using UnityEngine;

/// <summary>
/// Bootstrap for networked games: ensures <see cref="GameBootstrap.Locator"/> exists and runs Startup.Configure.
/// Use <see cref="GameBootstrap.Locator"/> only (no separate static here).
/// Required: one instance in scene (e.g. on same GameObject as GameNetworkManager). Runs before <see cref="GameBootstrap"/>.
/// </summary>
[DefaultExecutionOrder(-1001)]
public class NetworkedGameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (GameBootstrap.Locator == null)
            GameBootstrap.Locator = new ServiceLocator();
    }

    private void Start()
    {
        var locator = GameBootstrap.Locator;
        if (locator == null)
            return;

        CoreStartup.Configure(locator);
        CombatStartup.Configure(locator);
        PlayerStartup.Configure(locator);
        WorldStartup.Configure(locator);
        InventoryStartup.Configure(locator);
        ItemsStartup.Configure(locator);
        DataStartup.Configure(locator);
        UIStartup.Configure(locator);
        SceneStartup.Configure(locator);

        ServiceInitializer.ValidateAndLogServices(locator, requireNetworked: true);
    }
}
