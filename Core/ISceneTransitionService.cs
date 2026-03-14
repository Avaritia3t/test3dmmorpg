using UnityEngine;

public interface ISceneTransitionService
{
    void LoadHomeSceneBasedOnFaction(string faction);
    Vector3 GetSpawnPointForMap(string sceneName);
}
