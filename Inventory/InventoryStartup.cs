using Mirror;
using UnityEngine;

public static class InventoryStartup
{
    /// <summary>
    /// Prefer <see cref="NetworkedPlayerInventory"/> on the player prefab (registered at runtime for the local player via <see cref="NetworkedLocalPlayerServiceRegistrar"/>).
    /// Registers a scene <see cref="InventoryManager"/> only for offline / non-networked scenes (no <see cref="NetworkManager"/> in the scene).
    /// </summary>
    public static void Configure(ServiceLocator locator)
    {
        // Player prefab is not spawned yet — do not register a global InventoryManager when using Mirror.
        if (Object.FindObjectOfType<NetworkManager>() != null)
            return;

        var inventory = Object.FindObjectOfType<InventoryManager>();
        if (inventory != null)
            locator.Register<IInventoryService>(inventory);
    }
}
