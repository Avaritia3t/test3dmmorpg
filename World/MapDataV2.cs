using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMapDataV2", menuName = "Game/MapDataV2")]
public class MapDataV2 : ScriptableObject
{
    public string mapID;
    public string factionName;
    public Vector3 spawnPoint;
    public List<BuffV2> buffs; // Buffs for players of the map's faction
    public List<BuffV2> debuffs; // Debuffs for players of other factions
}
