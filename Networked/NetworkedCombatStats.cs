using UnityEngine;

/// <summary>
/// Serializable snapshot of combat stats used for server-side damage and effects.
/// Client sends this to the server so equipment and map buffs are reflected in networked combat.
/// </summary>
[System.Serializable]
public struct CombatStatsSnapshot
{
    public float attackSpeed;
    public float attackDamage;
    public float attackRange;
    public float penetration;
    public float integrity;
    public float damageReduction;
    public float statusResistance;
    public float criticalChance;
    public float criticalDamage;
    public float afflictionChance;
    public float afflictionDamage;
    public float etherealChance;
    public float etherealDamage;
    public float demonicChance;
    public float demonicDamage;
    public float inevitableChance;
    public float inevitableDamage;
    public float moveSpeed;

    public float baseHP;
    public float baseShield;

    public bool IsValid => attackSpeed > 0f || attackDamage > 0f;

    public static CombatStatsSnapshot From(PlayerStats stats)
    {
        if (stats == null)
            return default;

        return new CombatStatsSnapshot
        {
            attackSpeed = stats.attackSpeed,
            attackDamage = stats.currentDamage,
            attackRange = stats.attackRange,
            penetration = stats.penetration,
            integrity = stats.integrity,
            damageReduction = stats.damageReduction,
            statusResistance = stats.statusResistance,
            criticalChance = stats.criticalChance,
            criticalDamage = stats.criticalDamage,
            afflictionChance = stats.afflictionChance,
            afflictionDamage = stats.afflictionDamage,
            etherealChance = stats.etherealChance,
            etherealDamage = stats.etherealDamage,
            demonicChance = stats.demonicChance,
            demonicDamage = stats.demonicDamage,
            inevitableChance = stats.inevitableChance,
            inevitableDamage = stats.inevitableDamage,
            moveSpeed = stats.moveSpeed,
            baseHP = stats.baseHP,
            baseShield = stats.baseShield
        };
    }

    /// <summary>Defender snapshot for NPC subdomains (no PlayerStats resist fields yet).</summary>
    public static CombatStatsSnapshot FromSubdomain(NetworkedSubdomainController sub)
    {
        if (sub == null)
            return default;
        return new CombatStatsSnapshot
        {
            baseHP = sub.SubdomainHP,
            baseShield = sub.SubdomainShield,
            integrity = 0f,
            damageReduction = 0f,
            statusResistance = 0f
        };
    }
}
