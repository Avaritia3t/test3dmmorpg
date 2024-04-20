using System.Collections.Generic;
using UnityEngine;

public enum NPCType
{
    Anthropoid, Devourer, Martial, Terror, Haunt, Defiler, Commander
}

[System.Serializable]
public class NPC
{
    public NPCType type;
    public float hp;
    public float damageMin, damageMax;
    public List<Buff> buffs; // Make sure you have a Buff class defined
    public List<Resource> dropList;

    public NPC(NPCType type, float hp, float damageMin, float damageMax)
    {
        this.type = type;
        this.hp = hp;
        this.damageMin = damageMin;
        this.damageMax = damageMax;
        buffs = new List<Buff>();
        dropList = new List<Resource>();
    }
}
