using UnityEngine;

public static class SceneStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var canvasTransition = Object.FindFirstObjectByType<CanvasTransitionManager>();
        if (canvasTransition != null)
            locator.Register<ICanvasTransitionService>(canvasTransition);

        var sceneTransition = Object.FindFirstObjectByType<SceneTransitionManager>();
        if (sceneTransition != null)
            locator.Register<ISceneTransitionService>(sceneTransition);
    }
}
