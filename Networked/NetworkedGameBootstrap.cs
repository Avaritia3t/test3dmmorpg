using UnityEngine;

/// <summary>
/// Bootstrap for networked games: builds ServiceLocator and sets GameBootstrap.Locator so all scripts can use it.
/// Required: one instance in scene (e.g. on same GameObject as GameNetworkManager). Runs all Startup.Configure then validates services.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class NetworkedGameBootstrap : MonoBehaviour
{
    public static ServiceLocator Locator { get; private set; }

    private void Awake()
    {
        if (Locator == null)
            Locator = new ServiceLocator();
        // So scripts that reference GameBootstrap.Locator work in networked scenes
        GameBootstrap.Locator = Locator;
    }

    private void Start()
    {
        if (Locator == null)
            return;

        CoreStartup.Configure(Locator);
        CombatStartup.Configure(Locator);
        PlayerStartup.Configure(Locator);
        WorldStartup.Configure(Locator);
        InventoryStartup.Configure(Locator);
        ItemsStartup.Configure(Locator);
        DataStartup.Configure(Locator);
        UIStartup.Configure(Locator);
        SceneStartup.Configure(Locator);

        ServiceInitializer.ValidateAndLogServices(Locator, requireNetworked: true);
    }
}
