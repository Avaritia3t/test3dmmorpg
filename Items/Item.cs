using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Weapon, Phalanx, Artefact, Material, Relic, Currency
}

public enum WeaponType
{
    AncientLaser, OffLaser, HiTechLaser, RedLaser, WhiteLaser
}

public enum PhalanxType
{
    BlueGenerator, WhiteGenerator, RedGenerator, PurpleGenerator
}

public enum ArtefactType
{
    GoldenSkull, Microcosm
}


public enum ItemRarity
{
    Common, Uncommon, Rare, Epic, Legendary
}

public enum DamageType
{
    None, Physical, Ethereal, Demonic, Affliction, Inevitable
}

[System.Serializable]
public class ItemStat
{
    public string statName;
    public float statValue;

    public ItemStat(string name, float value)
    {
        statName = name;
        statValue = value;
    }
}

[System.Serializable]
public class Item
{
    /// <summary>Required for JSON persistence (e.g. SQLite equipment snapshot).</summary>
    public Item()
    {
        stats = new List<ItemStat>();
    }

    public string itemName;
    public ItemType itemType;
    public string subtype; // Subtype information
    public ItemRarity itemRarity;
    public string flavorText;
    public float generationRate; // Items generated per time unit
    public List<ItemStat> stats;
    public DamageType damageType; // Type of damage the item deals
    public float damageMin; // Minimum damage value
    public float damageMax; // Maximum damage value
    public int level; // Item level ranging from 1 to 20
    public Sprite icon; // Add this property to hold the item's icon
    public bool isEquippable;

    public Item(string itemName, ItemType itemType, string subtype, ItemRarity itemRarity, string flavorText, float generationRate, DamageType damageType, float damageMin, float damageMax, List<ItemStat> stats, int level = 1, Sprite icon = null, bool isEquippable = true)
    {
        this.itemName = itemName;
        this.itemType = itemType;
        this.subtype = subtype;
        this.itemRarity = itemRarity;
        this.flavorText = flavorText;
        this.generationRate = generationRate;
        this.damageType = damageType;
        this.damageMin = damageMin;
        this.damageMax = damageMax;
        this.stats = stats ?? new List<ItemStat>();
        this.level = level;
        this.icon = icon;
        this.isEquippable = isEquippable;
    }

    public void SetStats(ItemType? itemType = null)
    {
        SetItemType(itemType);
        SetItemRarity();
        SetDamageType();
        SetGenerationRate();
        SetLevel();
        SetDamageThresholds();
        // SetItemStats();
    }

    public void SetItemType(ItemType? type = null)
    {
        if (type.HasValue)
        {
            itemType = type.Value;
        }
        else
        {
            float roll = UnityEngine.Random.value;

            if (roll < 0.40f)
                itemType = ItemType.Material;
            else if (roll < 0.60f)
                itemType = ItemType.Weapon;
            else if (roll < 0.80f)
                itemType = ItemType.Phalanx;
            else if (roll < 0.95f)
                itemType = ItemType.Currency;
            else if (roll < 0.98f)
                itemType = ItemType.Artefact;
            else
                itemType = ItemType.Relic;
        }
    }


    public Dictionary<string, float> GetStats()
    {
        Dictionary<string, float> statDictionary = new Dictionary<string, float>();
        foreach (var stat in stats)
        {
            statDictionary.Add(stat.statName, stat.statValue);
        }
        return statDictionary;
    }


    public void SetItemRarity(ItemRarity? rarity = null)
    {
        if (rarity.HasValue)
        {
            itemRarity = rarity.Value;
        }
        else
        {
            float rarityRoll = UnityEngine.Random.value;

            switch (itemType)
            {
                case ItemType.Weapon:
                case ItemType.Phalanx:
                    if (rarityRoll < 0.40f)
                        itemRarity = ItemRarity.Common;
                    else if (rarityRoll < 0.70f)
                        itemRarity = ItemRarity.Uncommon;
                    else if (rarityRoll < 0.90f)
                        itemRarity = ItemRarity.Rare;
                    else if (rarityRoll < 0.97f)
                        itemRarity = ItemRarity.Epic;
                    else
                        itemRarity = ItemRarity.Legendary;
                    break;

                case ItemType.Artefact:
                case ItemType.Relic:
                    if (rarityRoll < 0.60f)
                        itemRarity = ItemRarity.Common;
                    else if (rarityRoll < 0.85f)
                        itemRarity = ItemRarity.Uncommon;
                    else if (rarityRoll < 0.95f)
                        itemRarity = ItemRarity.Rare;
                    else if (rarityRoll < 0.99f)
                        itemRarity = ItemRarity.Epic;
                    else
                        itemRarity = ItemRarity.Legendary;
                    break;

                case ItemType.Material:
                case ItemType.Currency:
                    if (rarityRoll < 0.70f)
                        itemRarity = ItemRarity.Common;
                    else if (rarityRoll < 0.85f)
                        itemRarity = ItemRarity.Uncommon;
                    else if (rarityRoll < 0.95f)
                        itemRarity = ItemRarity.Rare;
                    else if (rarityRoll < 0.98f)
                        itemRarity = ItemRarity.Epic;
                    else
                        itemRarity = ItemRarity.Legendary;
                    break;

                default:
                    itemRarity = ItemRarity.Common;
                    break;
            }
        }
    }

    public void SetGenerationRate()
    {
        switch (itemType)
        {
            case ItemType.Material:
            case ItemType.Currency:
                switch (itemRarity)
                {
                    case ItemRarity.Common:
                        generationRate = 5f;
                        break;
                    case ItemRarity.Uncommon:
                        generationRate = 2f;
                        break;
                    case ItemRarity.Rare:
                        generationRate = 0.1f;
                        break;
                    case ItemRarity.Epic:
                        generationRate = 0.01f;
                        break;
                    case ItemRarity.Legendary:
                        generationRate = 0.001f;
                        break;
                }
                break;

            case ItemType.Weapon:
            case ItemType.Phalanx:
                switch (itemRarity)
                {
                    case ItemRarity.Common:
                        generationRate = 2f;
                        break;
                    case ItemRarity.Uncommon:
                        generationRate = 1f;
                        break;
                    case ItemRarity.Rare:
                        generationRate = 0.2f;
                        break;
                    case ItemRarity.Epic:
                        generationRate = 0.01f;
                        break;
                    case ItemRarity.Legendary:
                        generationRate = 0.001f;
                        break;
                }
                break;

            case ItemType.Artefact:
                switch (itemRarity)
                {
                    case ItemRarity.Common:
                        generationRate = 0.1f;
                        break;
                    case ItemRarity.Uncommon:
                        generationRate = 0.03f;
                        break;
                    case ItemRarity.Rare:
                        generationRate = 0.01f;
                        break;
                    case ItemRarity.Epic:
                        generationRate = 0.001f;
                        break;
                    case ItemRarity.Legendary:
                        generationRate = 0.0005f;
                        break;
                }
                break;

            case ItemType.Relic:
                switch (itemRarity)
                {
                    case ItemRarity.Common:
                        generationRate = 0.1f;
                        break;
                    case ItemRarity.Uncommon:
                        generationRate = 0.03f;
                        break;
                    case ItemRarity.Rare:
                        generationRate = 0.01f;
                        break;
                    case ItemRarity.Epic:
                        generationRate = 0.001f;
                        break;
                    case ItemRarity.Legendary:
                        generationRate = 0.005f;
                        break;
                }
                break;
        }
    }

    public void SetDamageType(DamageType? dmgType = null)
    {
        if (dmgType.HasValue)
        {
            damageType = dmgType.Value;
        }
        else if (itemType == ItemType.Weapon || itemType == ItemType.Phalanx)
        {
            float roll = UnityEngine.Random.value;
            if (itemType == ItemType.Weapon)
            {
                if (roll < 0.40f)
                    damageType = DamageType.Physical;
                else if (roll < 0.80f)
                    damageType = DamageType.Ethereal;
                else if (roll < 0.96f)
                    damageType = DamageType.Demonic;
                else if (roll < 0.99f)
                    damageType = DamageType.Affliction;
                else
                    damageType = DamageType.Inevitable;
            }
            else if (itemType == ItemType.Phalanx)
            {
                if (roll < 0.30f)
                    damageType = DamageType.Physical;
                else if (roll < 0.80f)
                    damageType = DamageType.Ethereal;
                else if (roll < 0.97f)
                    damageType = DamageType.Demonic;
                else if (roll < 0.99f)
                    damageType = DamageType.Affliction;
                else
                    damageType = DamageType.Inevitable;
            }
        }
        else
        {
            damageType = DamageType.None;
        }
    }

    public void SetLevel(int? level = null)
    {
        if (level.HasValue && level.Value >= 1 && level.Value <= 20)
        {
            this.level = level.Value;
        }
        else
        {
            this.level = Random.Range(1, 21); // Random level between 1 and 20
        }
    }


    public void SetDamageThresholds()
    {
        if (itemType != ItemType.Weapon && itemType != ItemType.Phalanx)
        {
            return;
        }

        float baseMin = 0;
        float baseMax = 0;

        switch (damageType)
        {
            case DamageType.Physical:
                baseMin = 50;
                baseMax = 100;
                break;
            case DamageType.Ethereal:
                baseMin = 100;
                baseMax = 150;
                break;
            case DamageType.Demonic:
                baseMin = 150;
                baseMax = 200;
                break;
            case DamageType.Affliction:
                baseMin = 60;
                baseMax = 100;
                break;
            case DamageType.Inevitable:
                baseMin = 30;
                baseMax = 50;
                break;
            default:
                return; // Exit if the damage type is not set
        }

        // Apply subtype scaling (example: each subsequent subtype increases damage by 5%)
        int subtypeIndex = System.Array.IndexOf(System.Enum.GetValues(itemType.GetType()), subtype);
        float subtypeMultiplier = 1 + (0.05f * subtypeIndex);
        baseMin *= subtypeMultiplier;
        baseMax *= subtypeMultiplier;

        // Apply rarity scaling
        int rarityIndex = (int)itemRarity;
        baseMin += baseMin * (rarityIndex / 100f);
        baseMax += baseMax * (rarityIndex / 100f);

        // Apply level scaling
        baseMin += baseMin * (level * 0.005f);
        baseMax += baseMax * (level * 0.005f);

        // Set damageMin and damageMax
        damageMin = Mathf.Round(baseMin);
        damageMax = Mathf.Round(baseMax);
    }

    public void SetItemStats()
    {
        List<string> allowedStats = new List<string>();

        switch (itemType)
        {
            case ItemType.Weapon:
                allowedStats.AddRange(ItemStats.OffensiveStats);
                break;

            case ItemType.Phalanx:
                allowedStats.AddRange(ItemStats.DefensiveStats);
                break;

            default:
                return; // Exit if item type is neither Weapon nor Phalanx
        }

        // Add utility stats to both weapons and phalanxes
        allowedStats.AddRange(ItemStats.UtilityStats);

        // Initialize stats list if null
        stats = stats ?? new List<ItemStat>();

        // Randomly select and assign stats
        int numModifiers = UnityEngine.Random.Range(1, allowedStats.Count + 1);

        for (int i = 0; i < numModifiers; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, allowedStats.Count);
            string statName = allowedStats[randomIndex];
            allowedStats.RemoveAt(randomIndex); // Ensure no duplicate stats
            float statValue = UnityEngine.Random.Range(1f, 200f); // Random stat value
            stats.Add(new ItemStat(statName, statValue));
        }
    }

    public List<ItemStat> GenerateRandomStats(ItemType itemType, int maxStats, int subdomainLevel)
    {
        List<ItemStat> randomStats = new List<ItemStat>();
        List<string> statPool = new List<string>();

        // Add stats to the pool based on the item type
        switch (itemType)
        {
            case ItemType.Weapon:
                statPool.AddRange(ItemStats.OffensiveStats);
                break;
            case ItemType.Phalanx:
                statPool.AddRange(ItemStats.DefensiveStats);
                break;
            default:
                statPool.AddRange(ItemStats.UtilityStats);
                break;
        }

        // Limit the number of stats to the maxStats or available stats in the pool
        int numberOfStats = Mathf.Min(maxStats, statPool.Count);

        for (int i = 0; i < numberOfStats; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, statPool.Count);
            string statName = statPool[randomIndex];
            statPool.RemoveAt(randomIndex); // Ensure no duplicate stats

            float baseStatValue = UnityEngine.Random.Range(10, 100); // Base random value for the stat
            float scaledStatValue = baseStatValue * (1 + 0.02f * subdomainLevel); // Scale stat value by subdomain level
            randomStats.Add(new ItemStat(statName, scaledStatValue));
        }

        return randomStats;
    }
}
