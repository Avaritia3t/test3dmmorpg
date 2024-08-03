using UnityEngine;

[System.Serializable]
public class BuffV2
{
    public enum BuffType
    {
        Buff,
        Debuff
    }

    public BuffType Type;
    public string StatName;
    public float ModifierValue;
}
