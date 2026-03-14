using UnityEngine;

public interface IMapService
{
    MapDataV2 currentMap { get; }

    void SwitchMap(string mapID);
    void ApplyMapBuffs(GameObject player);
}
