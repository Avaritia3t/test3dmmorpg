using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "Game/Map")]
public class MapData : ScriptableObject
{
    public string mapID;
    public string FactionName;
    public Vector3 spawnPoint;
    public List<Buff> Buffs; // Assuming Buff is a class or struct you've defined elsewhere
    public string weatherEvent;
    // Add other properties as needed

}
