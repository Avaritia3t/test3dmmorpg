using UnityEngine;

/// <summary>Runs after <see cref="NetworkedGameBootstrap"/> when both exist (-1000 vs -1001).</summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static ServiceLocator Locator { get; set; }

    private void Awake()
    {
        // NetworkedGameBootstrap may have already created and assigned Locator.
        if (Locator == null)
            Locator = new ServiceLocator();
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

        ServiceInitializer.ValidateAndLogServices(Locator, requireNetworked: false);
    }
}
