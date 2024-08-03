using System.Collections.Generic;
using UnityEngine;

public enum RuneType
{
    GreenRune,
    BlueRune,
    YellowRune,
    RedRune,
    PurpleRune
}

[System.Serializable]
public class Rune
{
    public RuneType type;
    public int quantity;
    public float damageMultiplier;

    public Rune(RuneType type, int quantity, float damageMultiplier)
    {
        this.type = type;
        this.quantity = quantity;
        this.damageMultiplier = damageMultiplier;
    }
}
