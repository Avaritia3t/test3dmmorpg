using UnityEngine;

public static class WorldStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var networkedMap = Object.FindFirstObjectByType<NetworkedMapController>();
        if (networkedMap != null)
        {
            locator.Register<IMapService>(networkedMap);
            return;
        }
        var mapManager = Object.FindFirstObjectByType<MapManagerV3>();
        if (mapManager != null)
            locator.Register<IMapService>(mapManager);
    }
}
