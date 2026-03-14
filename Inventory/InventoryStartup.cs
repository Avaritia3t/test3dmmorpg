using UnityEngine;

public static class InventoryStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var inventory = Object.FindObjectOfType<InventoryManager>();
        if (inventory != null)
            locator.Register<IInventoryService>(inventory);
    }
}
