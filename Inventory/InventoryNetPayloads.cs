using System;
using UnityEngine;

/// <summary>Network row for <see cref="NetworkedPlayerInventory"/> (no Sprite; icon resolved locally).</summary>
[Serializable]
public class ItemPayload
{
    public string itemName;
    public ItemType itemType;
    public string subtype;
    public ItemRarity itemRarity;
    public string flavorText;
    public float generationRate;
    public DamageType damageType;
    public float damageMin;
    public float damageMax;
    public int level;
    public bool isEquippable;
    /// <summary>JsonUtility wrapper for <see cref="ItemStat"/> array.</summary>
    public string statsJson;
}

[Serializable]
public class ItemStatArrayWrapper
{
    public ItemStat[] stats;
}

/// <summary>Network resource row.</summary>
[Serializable]
public class ResourcePayload
{
    public ResourceType type;
    public ResourceGrade grade;
    public int quantity;
    public float regenerationSpeed;
}

/// <summary>Network rune row.</summary>
[Serializable]
public class RunePayload
{
    public RuneType type;
    public int quantity;
    public float damageMultiplier;
}

/// <summary>Converts between runtime <see cref="Item"/> and <see cref="ItemPayload"/> (icon resolved client-side).</summary>
public static class InventoryNetConverters
{
    public static ItemPayload ToPayload(Item item)
    {
        if (item == null)
            return null;

        var wrapper = new ItemStatArrayWrapper
        {
            stats = item.stats != null && item.stats.Count > 0 ? item.stats.ToArray() : Array.Empty<ItemStat>()
        };
        return new ItemPayload
        {
            itemName = item.itemName,
            itemType = item.itemType,
            subtype = item.subtype ?? "",
            itemRarity = item.itemRarity,
            flavorText = item.flavorText ?? "",
            generationRate = item.generationRate,
            damageType = item.damageType,
            damageMin = item.damageMin,
            damageMax = item.damageMax,
            level = item.level,
            isEquippable = item.isEquippable,
            statsJson = JsonUtility.ToJson(wrapper)
        };
    }

    public static Item FromPayload(ItemPayload p)
    {
        if (p == null)
            return null;
        var statsList = new System.Collections.Generic.List<ItemStat>();
        if (!string.IsNullOrEmpty(p.statsJson))
        {
            var w = JsonUtility.FromJson<ItemStatArrayWrapper>(p.statsJson);
            if (w?.stats != null)
            {
                foreach (var s in w.stats)
                    if (s != null)
                        statsList.Add(new ItemStat(s.statName, s.statValue));
            }
        }

        return new Item(
            p.itemName,
            p.itemType,
            p.subtype,
            p.itemRarity,
            p.flavorText,
            p.generationRate,
            p.damageType,
            p.damageMin,
            p.damageMax,
            statsList,
            p.level,
            null,
            p.isEquippable);
    }

    public static ResourcePayload ToPayload(Resource r)
    {
        if (r == null)
            return null;
        return new ResourcePayload
        {
            type = r.type,
            grade = r.grade,
            quantity = r.quantity,
            regenerationSpeed = r.regenerationSpeed
        };
    }

    public static Resource FromPayload(ResourcePayload p)
    {
        if (p == null)
            return null;
        return new Resource(p.type, p.grade, p.quantity, p.regenerationSpeed);
    }

    public static RunePayload ToPayload(Rune r)
    {
        if (r == null)
            return null;
        return new RunePayload
        {
            type = r.type,
            quantity = r.quantity,
            damageMultiplier = r.damageMultiplier
        };
    }

    public static Rune FromPayload(RunePayload p)
    {
        if (p == null)
            return null;
        return new Rune(p.type, p.quantity, p.damageMultiplier);
    }

    public static bool MatchesPayload(Item a, ItemPayload b)
    {
        if (a == null || b == null || string.IsNullOrEmpty(b.itemName))
            return false;
        return a.itemName == b.itemName && a.itemType == b.itemType && a.level == b.level && (a.subtype ?? "") == (b.subtype ?? "");
    }
}
