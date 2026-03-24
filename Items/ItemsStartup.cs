using UnityEngine;

public static class ItemsStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var dropRules = Object.FindFirstObjectByType<DropRules>();
        if (dropRules != null)
            locator.Register<IDropRulesService>(dropRules);
    }
}
