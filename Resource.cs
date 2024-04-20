using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Lumber, Stone, Sand, Food, Textiles, Metals, Spirit, Coin,
    Tectonite, Miasma, Syntheos, Tinitum, Teria, Esseo
}

public enum ResourceGrade
{
    Usable, Standard, Superior, Reserved
}

[System.Serializable]
public class Resource
{
    public ResourceType type;
    public ResourceGrade grade;
    public int quantity;
    public float regenerationSpeed;

    public Resource(ResourceType type, ResourceGrade grade, int quantity, float regenerationSpeed)
    {
        this.type = type;
        this.grade = grade;
        this.quantity = quantity;
        this.regenerationSpeed = regenerationSpeed;
    }

    public Resource(ResourceType type, int quantity)
    {
        this.type = type;
        this.grade = ResourceGrade.Standard; // Default grade for resources without specific grades
        this.quantity = quantity;
    }
}
