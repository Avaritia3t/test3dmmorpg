using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropRule
{
    public SubdomainV2Type subdomainType;
    public List<ResourceType> allowedResources;
    public List<ItemType> allowedItemTypes;

    public DropRule(SubdomainV2Type subdomainType, List<ResourceType> allowedResources, List<ItemType> allowedItemTypes)
    {
        this.subdomainType = subdomainType;
        this.allowedResources = allowedResources;
        this.allowedItemTypes = allowedItemTypes;
    }
}

public class DropRules : MonoBehaviour
{
    public static DropRules Instance { get; private set; }
    public List<DropRule> dropRules;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeDropRules();
        // Debug.Log("DropRules instance initialized.");
    }

    private void InitializeDropRules()
    {
        dropRules = new List<DropRule>
        {
            new DropRule(SubdomainV2Type.Badlands, new List<ResourceType> { ResourceType.Lumber, ResourceType.Stone, ResourceType.Sand, ResourceType.Food }, new List<ItemType>()),
            new DropRule(SubdomainV2Type.Hovel, new List<ResourceType> { ResourceType.Lumber, ResourceType.Stone, ResourceType.Sand, ResourceType.Food, ResourceType.Textiles }, new List<ItemType>()),
            new DropRule(SubdomainV2Type.Hearthstead, new List<ResourceType> { ResourceType.Lumber, ResourceType.Stone, ResourceType.Sand, ResourceType.Food, ResourceType.Textiles }, new List<ItemType>()),
            new DropRule(SubdomainV2Type.Thorp, new List<ResourceType> { ResourceType.Lumber, ResourceType.Stone, ResourceType.Sand, ResourceType.Food, ResourceType.Textiles, ResourceType.Metals, ResourceType.Spirit }, new List<ItemType>()),
            new DropRule(SubdomainV2Type.Borough, new List<ResourceType> { ResourceType.Food, ResourceType.Textiles, ResourceType.Metals, ResourceType.Spirit, ResourceType.Coin }, new List<ItemType>()),
            new DropRule(SubdomainV2Type.Civicron, new List<ResourceType> { ResourceType.Food, ResourceType.Textiles, ResourceType.Metals, ResourceType.Spirit, ResourceType.Coin }, new List<ItemType>()),
            new DropRule(SubdomainV2Type.Arcanopolis, new List<ResourceType> { ResourceType.Food, ResourceType.Spirit }, new List<ItemType> { ItemType.Weapon }),
            new DropRule(SubdomainV2Type.Dominionhold, new List<ResourceType> { ResourceType.Spirit, ResourceType.Coin, ResourceType.Tectonite }, new List<ItemType> { ItemType.Phalanx }),
            new DropRule(SubdomainV2Type.Sovereignty, new List<ResourceType> { ResourceType.Spirit, ResourceType.Coin, ResourceType.Tectonite }, new List<ItemType> { ItemType.Weapon, ItemType.Phalanx }),
            new DropRule(SubdomainV2Type.Apex, new List<ResourceType> { ResourceType.Tectonite }, new List<ItemType> { ItemType.Artefact })
        };
        // Debug.Log("DropRules initialized with " + dropRules.Count + " rules.");
    }

    public static Item GenerateEmptyItem(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                var weaponSubtypes = new List<WeaponType> { WeaponType.AncientLaser, WeaponType.OffLaser, WeaponType.HiTechLaser, WeaponType.RedLaser, WeaponType.WhiteLaser };
                var selectedWeaponSubtype = weaponSubtypes[UnityEngine.Random.Range(0, weaponSubtypes.Count)];
                return new Item
                (
                    itemName: selectedWeaponSubtype.ToString(),
                    itemType: itemType,
                    subtype: selectedWeaponSubtype.ToString(),
                    itemRarity: (ItemRarity)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ItemRarity)).Length),
                    flavorText: $"A {selectedWeaponSubtype} weapon",
                    generationRate: 0.01f,
                    damageType: DamageType.Physical,
                    damageMin: 0,
                    damageMax: 0,
                    stats: new List<ItemStat>(),
                    level: 1,
                    icon: null
                );

            case ItemType.Phalanx:
                var phalanxSubtypes = new List<PhalanxType> { PhalanxType.BlueGenerator, PhalanxType.WhiteGenerator, PhalanxType.RedGenerator, PhalanxType.PurpleGenerator };
                var selectedPhalanxSubtype = phalanxSubtypes[UnityEngine.Random.Range(0, phalanxSubtypes.Count)];
                return new Item
                (
                    itemName: selectedPhalanxSubtype.ToString(),
                    itemType: itemType,
                    subtype: selectedPhalanxSubtype.ToString(),
                    itemRarity: (ItemRarity)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ItemRarity)).Length),
                    flavorText: $"A {selectedPhalanxSubtype} phalanx",
                    generationRate: 0.01f,
                    damageType: DamageType.Physical,
                    damageMin: 0,
                    damageMax: 0,
                    stats: new List<ItemStat>(),
                    level: 1,
                    icon: null
                );

            case ItemType.Artefact:
                var artefactSubtypes = new List<ArtefactType> { ArtefactType.GoldenSkull, ArtefactType.Microcosm };
                var selectedArtefactSubtype = artefactSubtypes[UnityEngine.Random.Range(0, artefactSubtypes.Count)];
                return new Item
                (
                    itemName: selectedArtefactSubtype.ToString(),
                    itemType: itemType,
                    subtype: selectedArtefactSubtype.ToString(),
                    itemRarity: (ItemRarity)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ItemRarity)).Length),
                    flavorText: $"A {selectedArtefactSubtype} artefact",
                    generationRate: 0.01f,
                    damageType: DamageType.Ethereal,
                    damageMin: 0,
                    damageMax: 0,
                    stats: new List<ItemStat>(),
                    level: 1,
                    icon: null
                );

            default:
                throw new System.ArgumentException("Invalid item type");
        }
    }
}
