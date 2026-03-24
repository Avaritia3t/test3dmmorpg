using System.Collections.Generic;

public class PlayerStats
{
    // This class is for back-end logical storage and manipulation of player statistics
    public string faction { get; set; }

    /// <summary>Setup flow: set when the player confirms class (local profile DB; server auth is separate).</summary>
    public bool hasSelectedClass { get; set; }

    /// <summary>Setup flow: set when the player confirms faction.</summary>
    public bool hasSelectedFaction { get; set; }

    public string className { get; set; } = "";
    public float baseHP { get; set; } = 1000f;
    public float currentHP { get; set; }
    public float baseShield { get; set; } = 5000f;
    public float currentShield { get; set; }
    public float attackSpeed { get; set; } = 300f;
    public float currentDamage { get; set; } = 500f;
    public float attackRange { get; set; } = 200f;
    public string playerName { get; set; } = "Sol";
    public int experience { get; set; }
    public int level { get; set; }
    public float combatRegenDelay { get; set; } = 10f;
    public float shieldRegenPercent { get; set; } = 4f;
    public float hpRegenPercent { get; set; } = 2.5f;

    // Additional stats
    public float criticalChance { get; set; }
    public float criticalDamage { get; set; }
    public float allDamage { get; set; }
    public float physicalDamage { get; set; }
    public float etherealDamage { get; set; }
    public float demonicDamage { get; set; }
    public float afflictionDamage { get; set; }
    public float inevitableDamage { get; set; }
    public float maxHP { get; set; }
    public float maxShield { get; set; }
    public float moveSpeed { get; set; }
    public float expGain { get; set; }
    public float shieldAbsorption { get; set; }
    public float hpRegeneration { get; set; }
    public float shieldRegeneration { get; set; }
    public float hitRate { get; set; }
    public float penetration { get; set; }
    public float integrity { get; set; }
    public float evasion { get; set; }
    public float stunChance { get; set; }
    public float statusResistance { get; set; }
    public float damageReduction { get; set; }
    public float physicalLifesteal { get; set; }
    public float magicalLifesteal { get; set; }
    public float stewardship { get; set; }
    public float stealth { get; set; }
    public float awareness { get; set; }
    public float etherealChance { get; set; }
    public float demonicChance { get; set; }
    public float afflictionChance { get; set; }
    public float inevitableChance { get; set; }

    // Equipment slots
    public List<EquipmentSlot> equipmentSlots;

    public PlayerStats()
    {
        currentHP = baseHP;
        currentShield = baseShield;
        experience = 0;
        level = 1;

        faction = "";
        className = "";
        hasSelectedClass = false;
        hasSelectedFaction = false;

        // Initialize additional stats with default values
        criticalChance = 0f;
        criticalDamage = 0f;
        allDamage = 0f;
        physicalDamage = 0f;
        etherealDamage = 0f;
        demonicDamage = 0f;
        afflictionDamage = 0f;
        inevitableDamage = 0f;
        maxHP = baseHP;
        maxShield = baseShield;
        moveSpeed = 0f;
        expGain = 0f;
        shieldAbsorption = 0f;
        hpRegeneration = 0f;
        shieldRegeneration = 0f;
        hitRate = 0f;
        penetration = 0f;
        integrity = 0f;
        evasion = 0f;
        stunChance = 0f;
        statusResistance = 0f;
        damageReduction = 0f;
        physicalLifesteal = 0f;
        magicalLifesteal = 0f;
        stewardship = 0f;
        stealth = 0f;
        awareness = 0f;
        etherealChance = 0f;
        demonicChance = 0f;
        afflictionChance = 0f;
        inevitableChance = 0f;

        InitializeEquipmentSlots();
    }

    public void InitializeEquipmentSlots()
    {
        equipmentSlots = new List<EquipmentSlot>
        {
            new EquipmentSlot(EquipmentSlotType.Weapon, 20, 5),
            new EquipmentSlot(EquipmentSlotType.Phalanx, 20, 3),
            new EquipmentSlot(EquipmentSlotType.Artefact, 5, 1),
            new EquipmentSlot(EquipmentSlotType.Relic, 3, 0),
            new EquipmentSlot(EquipmentSlotType.Blessing, 1, 0)
        };
    }
}
