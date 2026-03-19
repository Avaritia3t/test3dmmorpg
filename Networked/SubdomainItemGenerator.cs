using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates items for subdomains (stats, subtype, icon, damage type). Used by NetworkedSubdomainController.
/// Optional: on subdomain prefab, same GameObject as NetworkedSubdomainController. No scene placement.
/// </summary>
public class SubdomainItemGenerator : MonoBehaviour
{
    /// <summary>Builds initial available items for a subdomain from drop rules and assigns stats.</summary>
    public List<Item> InitializeItems(DropRule rule, IDropRulesService dropRulesService, int level)
    {
        var availableItems = new List<Item>();
        if (rule == null || dropRulesService == null) return availableItems;

        foreach (ItemType itemType in rule.allowedItemTypes)
        {
            Item newItem = dropRulesService.GenerateEmptyItem(itemType);
            if (newItem != null)
            {
                AssignItemStatsBySubdomain(newItem, rule.subdomainType, level);
                availableItems.Add(newItem);
            }
        }
        return availableItems;
    }

    /// <summary>Creates one full item (subtype, icon, stats) for periodic spawns.</summary>
    public Item GenerateItemForSubdomain(ItemType itemType, SubdomainV2Type subdomainType, int level)
    {
        Item item = CreateRandomItem(itemType, level);
        if (item != null)
            AssignItemStatsBySubdomain(item, subdomainType, level);
        return item;
    }

    private void AssignItemStatsBySubdomain(Item item, SubdomainV2Type subdomainType, int level)
    {
        int maxStats = 0;
        var statPool = new List<string>();

        switch (subdomainType)
        {
            case SubdomainV2Type.Arcanopolis:
                maxStats = 2;
                statPool.AddRange(ItemStats.OffensiveStats);
                break;
            case SubdomainV2Type.Dominionhold:
                maxStats = 3;
                statPool.AddRange(ItemStats.DefensiveStats);
                break;
            case SubdomainV2Type.Sovereignty:
                maxStats = 4;
                statPool.AddRange(ItemStats.OffensiveStats);
                statPool.AddRange(ItemStats.DefensiveStats);
                break;
            case SubdomainV2Type.Apex:
                maxStats = 5;
                statPool.AddRange(ItemStats.OffensiveStats);
                statPool.AddRange(ItemStats.DefensiveStats);
                statPool.AddRange(ItemStats.UtilityStats);
                break;
        }

        int numStats = Mathf.Min(Random.Range(1, maxStats + 1), statPool.Count);
        for (int i = 0; i < numStats; i++)
        {
            int randomIndex = Random.Range(0, statPool.Count);
            string statName = statPool[randomIndex];
            statPool.RemoveAt(randomIndex);
            float baseValue = Random.Range(10f, 100f);
            float scaledValue = baseValue * (1 + (0.02f * level));
            item.stats.Add(new ItemStat(statName, scaledValue));
        }
    }

    private Item CreateRandomItem(ItemType itemType, int level)
    {
        string subtype = GenerateRandomSubtype(itemType);
        string iconPath = $"EquipmentIcons/{subtype.ToLower()}";
        Sprite icon = Resources.Load<Sprite>(iconPath);
        if (icon == null)
        {
            // Debug.Log($"[SubdomainItemGenerator] Icon not found at path: {iconPath}");
        }

        return new Item(
            itemName: subtype,
            itemType: itemType,
            subtype: subtype,
            itemRarity: (ItemRarity)Random.Range(0, System.Enum.GetValues(typeof(ItemRarity)).Length),
            flavorText: $"A {subtype} {itemType}",
            generationRate: 0.01f,
            damageType: (itemType == ItemType.Weapon || itemType == ItemType.Phalanx) ? GenerateRandomDamageType() : DamageType.None,
            damageMin: 0,
            damageMax: 0,
            stats: new List<ItemStat>(),
            level: level,
            icon: icon
        );
    }

    private string GenerateRandomSubtype(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                var weaponSubtypes = new List<string> { "AncientLaser", "OffLaser", "HiTechLaser", "RedLaser", "WhiteLaser" };
                return weaponSubtypes[Random.Range(0, weaponSubtypes.Count)];
            case ItemType.Phalanx:
                var phalanxSubtypes = new List<string> { "BlueGenerator", "WhiteGenerator", "RedGenerator", "PurpleGenerator" };
                return phalanxSubtypes[Random.Range(0, phalanxSubtypes.Count)];
            case ItemType.Artefact:
                var artefactSubtypes = new List<string> { "GoldenSkull", "Microcosm" };
                return artefactSubtypes[Random.Range(0, artefactSubtypes.Count)];
            default:
                return "UnknownSubtype";
        }
    }

    private DamageType GenerateRandomDamageType()
    {
        float roll = Random.value;
        if (roll < 0.40f) return DamageType.Physical;
        if (roll < 0.80f) return DamageType.Ethereal;
        if (roll < 0.96f) return DamageType.Demonic;
        if (roll < 0.99f) return DamageType.Affliction;
        return DamageType.Inevitable;
    }
}
