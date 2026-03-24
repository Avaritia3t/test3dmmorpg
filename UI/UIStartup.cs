using UnityEngine;

public static class UIStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var tooltip = Object.FindFirstObjectByType<TooltipManager>();
        if (tooltip != null)
            locator.Register<ITooltipService>(tooltip);
    }
}
