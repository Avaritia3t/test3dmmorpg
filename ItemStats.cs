using System.Collections.Generic;

public static class ItemStats
{
    public static readonly List<string> OffensiveStats = new List<string>
    {
        "criticalChance", "criticalDamage", "allDamage", "physicalDamage", "etherealDamage",
        "demonicDamage", "afflictionDamage", "inevitableDamage", "attackSpeed",
        "hitRate", "penetration", "stunChance", "physicalLifesteal", "magicalLifesteal",
        "etherealChance", "demonicChance", "afflictionChance", "inevitableChance"
    };

    public static readonly List<string> DefensiveStats = new List<string>
    {
        "maxHP", "maxShield", "moveSpeed", "shieldAbsorption",
        "hpRegeneration", "shieldRegeneration", "integrity", "evasion",
        "statusResistance", "damageReduction", "stealth"
    };

    public static readonly List<string> UtilityStats = new List<string>
    {
        "expGain", "stewardship", "awareness"
    };
}
