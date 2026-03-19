using UnityEngine;

public static class WorldStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var networkedMap = Object.FindObjectOfType<NetworkedMapController>();
        if (networkedMap != null)
        {
            locator.Register<IMapService>(networkedMap);
            return;
        }
        var mapManager = Object.FindObjectOfType<MapManagerV3>();
        if (mapManager != null)
            locator.Register<IMapService>(mapManager);
    }
}
