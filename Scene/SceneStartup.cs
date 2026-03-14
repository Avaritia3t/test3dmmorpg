using UnityEngine;

public static class SceneStartup
{
    public static void Configure(ServiceLocator locator)
    {
        var canvasTransition = Object.FindObjectOfType<CanvasTransitionManager>();
        if (canvasTransition != null)
            locator.Register<ICanvasTransitionService>(canvasTransition);

        var sceneTransition = Object.FindObjectOfType<SceneTransitionManager>();
        if (sceneTransition != null)
            locator.Register<ISceneTransitionService>(sceneTransition);
    }
}
