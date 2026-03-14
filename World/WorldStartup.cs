using UnityEngine;

public static class WorldStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var mapManager = Object.FindObjectOfType<MapManagerV3>();
        if (mapManager != null)
            locator.Register<IMapService>(mapManager);
    }
}
