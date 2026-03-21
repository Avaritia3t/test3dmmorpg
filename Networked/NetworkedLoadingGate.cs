using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Async scene load + Mirror client ready gate. Use from UI flow (e.g. after faction selection)
/// while staying on a background canvas until the game scene and network are ready.
/// </summary>
public static class NetworkedLoadingGate
{
    /// <summary>
    /// Last additive world scene loaded by <see cref="SwitchAdditiveWorld"/> / <see cref="WaitForAsyncLoadAndMirrorReady"/> (for debugging or explicit unload).
    /// </summary>
    public static string LastLoadedAdditiveWorldScene { get; private set; }

    /// <summary>
    /// Unloads an additively loaded scene by name if it is currently loaded. Does not touch the active single-load "menu" scene.
    /// After unload, reset pooled / scene-bound state (e.g. <see cref="INetworkedAttackHandlerPool"/>) in scene-specific code if needed.
    /// </summary>
    public static IEnumerator UnloadAdditiveSceneIfLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            yield break;

        var op = SceneManager.UnloadSceneAsync(scene);
        if (op != null)
        {
            while (!op.isDone)
                yield return null;
        }

        if (LastLoadedAdditiveWorldScene == sceneName)
            LastLoadedAdditiveWorldScene = null;
    }

    /// <summary>
    /// Prototype helper: unload the previous additive world (if any), then load the next and optionally wait for Mirror ready.
    /// Tracks <see cref="LastLoadedAdditiveWorldScene"/> for the loaded scene.
    /// </summary>
    public static IEnumerator SwitchAdditiveWorld(string previousWorldSceneToUnload, string newWorldSceneToLoad, bool waitForMirrorReady = true)
    {
        if (!string.IsNullOrEmpty(previousWorldSceneToUnload))
            yield return UnloadAdditiveSceneIfLoaded(previousWorldSceneToUnload);

        yield return WaitForAsyncLoadAndMirrorReady(newWorldSceneToLoad, waitForMirrorReady);
    }

    /// <summary>
    /// Loads a scene additively (optional), then optionally waits until Mirror client is ready (if a client is active).
    /// Sets <see cref="LastLoadedAdditiveWorldScene"/> when a scene name is provided.
    /// </summary>
    public static IEnumerator WaitForAsyncLoadAndMirrorReady(string additiveSceneName, bool waitForMirrorReady = true)
    {
        if (!string.IsNullOrEmpty(additiveSceneName))
        {
            var op = SceneManager.LoadSceneAsync(additiveSceneName, LoadSceneMode.Additive);
            if (op != null)
            {
                while (!op.isDone)
                    yield return null;
            }

            LastLoadedAdditiveWorldScene = additiveSceneName;
        }

        if (waitForMirrorReady)
            yield return WaitForMirrorClientReadyIfNeeded();
    }

    /// <summary>Waits while NetworkClient exists but is not yet ready (post-connect scene load).</summary>
    public static IEnumerator WaitForMirrorClientReadyIfNeeded()
    {
        if (!NetworkClient.active)
            yield break;

        float timeout = 120f;
        float t = 0f;
        while (NetworkClient.active && !NetworkClient.ready && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
