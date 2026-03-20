using UnityEngine;

/// <summary>
/// Server-side dispatcher: applies an <see cref="AbilityEffectDefinition"/> to a target.
/// It routes only the implemented effect types into <see cref="NetworkedStatusEffectController"/>.
///
/// Later: we can extend with non-status effects (shield siphon, miss chance, inventory steal, etc.)
/// by adding more effect types and corresponding handlers.
/// </summary>
public static class AbilityEffectApplier
{
    public static void ApplyToTarget(
        GameObject target,
        string sourceAbilityId, // ICD key: which ability is allowed to apply first tick damage
        CombatStatsSnapshot attackerSnapshot,
        AbilityEffectDefinition effect,
        GameObject attackerObject)
    {
        if (target == null || effect == null)
            return;

        // For percent-of-target-maxHP damage formulas we need the defender base HP.
        if (!TryGetDefenderSnapshot(target, out var defenderSnapshot))
        {
            // If target doesn't support NetworkedDomainController snapshots, we can't resolve % damage yet.
            return;
        }

        var status = target.GetComponent<NetworkedStatusEffectController>();
        if (status == null)
            return;

        switch (effect.Type)
        {
            case AbilityEffectDefinition.EffectType.AfflictionDoT:
            {
                float flat = effect.DoTFlatDamageConstantPerTick;
                if (!string.IsNullOrEmpty(effect.DoTFlatDamageStatSourceNamePerTick))
                {
                    if (TryResolveAttackerStatValue(attackerSnapshot, effect.DoTFlatDamageStatSourceNamePerTick, out var resolved))
                        flat = resolved;
                }

                float percentPoints = effect.DoTPercentOfTargetBaseHPConstantPerTick;
                if (!string.IsNullOrEmpty(effect.DoTPercentOfTargetBaseHPStatSourceNamePerTick))
                {
                    if (TryResolveAttackerStatValue(attackerSnapshot, effect.DoTPercentOfTargetBaseHPStatSourceNamePerTick, out var resolved))
                        percentPoints = resolved;
                }

                float percentComponent = defenderSnapshot.baseHP * (percentPoints / 100f);
                float rawTickDamage = (flat + percentComponent) * effect.DoTDamageMultiplierPerTick;

                status.ApplyAffliction(
                    abilityId: sourceAbilityId,
                    attackerSnapshot: attackerSnapshot,
                    damageSource: attackerObject,
                    baseTickDamage: rawTickDamage,
                    baseDurationSeconds: effect.DurationSeconds,
                    tickIntervalSeconds: effect.DamageIntervalSeconds,
                    internalDamageCooldownSeconds: effect.InternalDamageCooldownSeconds);
                break;
            }

            case AbilityEffectDefinition.EffectType.InevitableDelayedDamage:
            {
                float flat = effect.InevitableFlatDamageConstantOnce;
                if (!string.IsNullOrEmpty(effect.InevitableFlatDamageStatSourceNameOnce))
                {
                    if (TryResolveAttackerStatValue(attackerSnapshot, effect.InevitableFlatDamageStatSourceNameOnce, out var resolved))
                        flat = resolved;
                }

                float percentPoints = effect.InevitablePercentOfTargetBaseHPConstantOnce;
                if (!string.IsNullOrEmpty(effect.InevitablePercentOfTargetBaseHPStatSourceNameOnce))
                {
                    if (TryResolveAttackerStatValue(attackerSnapshot, effect.InevitablePercentOfTargetBaseHPStatSourceNameOnce, out var resolved))
                        percentPoints = resolved;
                }

                float percentComponent = defenderSnapshot.baseHP * (percentPoints / 100f);
                float rawOnceDamage = (flat + percentComponent) * effect.InevitableDamageMultiplierOnce;

                status.ApplyInevitable(
                    abilityId: sourceAbilityId,
                    attackerSnapshot: attackerSnapshot,
                    damageSource: attackerObject,
                    baseDamage: rawOnceDamage,
                    baseDelaySeconds: effect.DelaySeconds,
                    internalDamageCooldownSeconds: effect.InternalDamageCooldownSeconds);
                break;
            }

            case AbilityEffectDefinition.EffectType.Slow:
                status.ApplySlow(
                    abilityId: sourceAbilityId,
                    slowMultiplier: effect.SlowMultiplier,
                    baseDurationSeconds: effect.DurationSeconds,
                    rooted: false);
                break;

            case AbilityEffectDefinition.EffectType.Root:
                status.ApplySlow(
                    abilityId: sourceAbilityId,
                    slowMultiplier: effect.SlowMultiplier,
                    baseDurationSeconds: effect.DurationSeconds,
                    rooted: true);
                break;
        }
    }

    private static bool TryGetDefenderSnapshot(GameObject target, out CombatStatsSnapshot snapshot)
    {
        snapshot = default;
        if (target == null) return false;

        var domain = target.GetComponent<NetworkedDomainController>();
        if (domain != null)
        {
            snapshot = domain.GetCombatStatsSnapshot();
            return snapshot.baseHP > 0f;
        }

        var sub = target.GetComponent<NetworkedSubdomainController>();
        if (sub != null)
        {
            snapshot = CombatStatsSnapshot.FromSubdomain(sub);
            return snapshot.baseHP > 0f;
        }

        return false;
    }

    /// <summary>
    /// Resolves an attacker stat value by name from <see cref="CombatStatsSnapshot"/>.
    /// Supported stat names are the field names exposed in the snapshot.
    /// </summary>
    private static bool TryResolveAttackerStatValue(CombatStatsSnapshot attacker, string statName, out float value)
    {
        value = 0f;
        if (string.IsNullOrEmpty(statName))
            return false;

        switch (statName.Trim())
        {
            case "attackSpeed": value = attacker.attackSpeed; return true;
            case "attackDamage": value = attacker.attackDamage; return true;
            case "currentDamage": value = attacker.attackDamage; return true;
            case "attackRange": value = attacker.attackRange; return true;

            case "penetration": value = attacker.penetration; return true;
            case "integrity": value = attacker.integrity; return true;
            case "damageReduction": value = attacker.damageReduction; return true;
            case "statusResistance": value = attacker.statusResistance; return true;

            case "criticalChance": value = attacker.criticalChance; return true;
            case "criticalDamage": value = attacker.criticalDamage; return true;

            case "afflictionChance": value = attacker.afflictionChance; return true;
            case "afflictionDamage": value = attacker.afflictionDamage; return true;

            case "etherealChance": value = attacker.etherealChance; return true;
            case "etherealDamage": value = attacker.etherealDamage; return true;

            case "demonicChance": value = attacker.demonicChance; return true;
            case "demonicDamage": value = attacker.demonicDamage; return true;

            case "inevitableChance": value = attacker.inevitableChance; return true;
            case "inevitableDamage": value = attacker.inevitableDamage; return true;

            case "moveSpeed": value = attacker.moveSpeed; return true;
        }

        return false;
    }
}

