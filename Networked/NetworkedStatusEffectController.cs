using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Server-side status effects runtime for networked combat units (player or subdomain).
/// Required: attach to prefab with NetworkedDomainController and/or NetworkedSubdomainController.
///
/// First pass implemented mechanics:
/// - Affliction DoT with separate debuff duration and ability-damage cooldown (ICD).
///   Reapplying refreshes duration to full, but the "first tick damage" is gated by ICD.
/// - Slow movement modifier with refresh-to-full and optional root override.
/// </summary>
public class NetworkedStatusEffectController : MonoBehaviour
{
    private NetworkedDomainController domain;
    private NetworkedSubdomainController subdomain;
    private GameObject damageSource;

    private class AfflictionDotRuntime
    {
        public float remainingDuration;
        public float tickInterval;
        public float nextTickTime;

        // Resolved at (re)apply time so DoT ticks don't change as resist stats change mid-effect.
        public float hpTickDamage;
        public float shieldTickDamage;

        public float damageInternalCooldownSeconds;

        // Tracks ICD per abilityId to support multiple effect sources later.
        public readonly Dictionary<string, float> lastDamageAppliedByAbilityId = new Dictionary<string, float>();

        // If false, the effect exists but hasn't scheduled ticking yet (due to ICD blocking first tick).
        public bool tickingArmed;
    }

    private class SlowRuntime
    {
        public float remainingDuration;
        public float slowMultiplier;
        public bool rooted;
    }

    private AfflictionDotRuntime affliction;
    private InevitableDelayedDamageRuntime inevitable;
    private SlowRuntime slow;

    private class InevitableDelayedDamageRuntime
    {
        public float remainingDuration;
        public float nextDamageTime;

        public float hpOnceDamage;
        public float shieldOnceDamage;

        public float internalDamageCooldownSeconds;
        public readonly Dictionary<string, float> lastDamageAppliedByAbilityId = new Dictionary<string, float>();

        public bool damageApplied;
    }

    private void Awake()
    {
        domain = GetComponent<NetworkedDomainController>();
        subdomain = GetComponent<NetworkedSubdomainController>();
    }

    private void Update()
    {
        if (!NetworkServer.active)
            return;

        TickAffliction();
        TickInevitable();
        TickSlow();
    }

    public void ApplyAffliction(
        string abilityId,
        CombatStatsSnapshot attackerSnapshot,
        GameObject damageSource,
        float baseTickDamage,
        float baseDurationSeconds,
        float tickIntervalSeconds,
        float internalDamageCooldownSeconds)
    {
        if (domain == null && subdomain == null)
            return;

        this.damageSource = damageSource;

        var defenderSnapshot = GetDefenderSnapshot();

        // Refresh debuff uptime to full (then ICD-gate the immediate "first tick damage" from this reapply).
        float durationMultiplier = DurationMultiplierFromStatusResistance(defenderSnapshot.statusResistance);
        float effectiveDuration = Mathf.Max(0f, baseDurationSeconds * durationMultiplier);

        // Render tick damage once at (re)apply time.
        // Affliction is a unique damage type => statusResistance reduces damage (but inevitable does not).
        float uniqueDamageMultiplier = UniqueDamageMultiplierFromStatusResistance(defenderSnapshot.statusResistance);
        float renderedTickDamage = baseTickDamage * uniqueDamageMultiplier;

        var (hpTick, shieldTick) = ResolveHpShieldDamage(
            attackerSnapshot,
            defenderSnapshot,
            renderedTickDamage,
            isPhysicalDamage: false); // Affliction ignores flat damageReduction in your rules.

        if (affliction == null)
            affliction = new AfflictionDotRuntime();

        affliction.remainingDuration = effectiveDuration;
        affliction.tickInterval = tickIntervalSeconds;
        affliction.hpTickDamage = hpTick;
        affliction.shieldTickDamage = shieldTick;
        affliction.damageInternalCooldownSeconds = internalDamageCooldownSeconds;

        bool canApplyFirstTick = CanApplyAbilityDamageNow(affliction, abilityId, internalDamageCooldownSeconds);
        if (canApplyFirstTick)
        {
            ApplyTickDamage();
            MarkAbilityDamageApplied(affliction, abilityId);

            affliction.nextTickTime = Time.time + affliction.tickInterval;
            affliction.tickingArmed = true;
        }
        else
        {
            // ICD says "no first tick damage from this reapply", but we still refresh duration.
            // If ticking hasn't started yet, arm it to begin on the next interval tick.
            if (!affliction.tickingArmed)
            {
                affliction.nextTickTime = Time.time + affliction.tickInterval;
                affliction.tickingArmed = true;
            }
        }
    }

    public void ApplySlow(
        string abilityId,
        float slowMultiplier,
        float baseDurationSeconds,
        bool rooted)
    {
        if (domain == null)
            return;

        var defenderSnapshot = GetDefenderSnapshot();
        float durationMultiplier = DurationMultiplierFromStatusResistance(defenderSnapshot.statusResistance);
        float effectiveDuration = Mathf.Max(0f, baseDurationSeconds * durationMultiplier);

        if (slow == null)
            slow = new SlowRuntime();

        slow.remainingDuration = effectiveDuration;
        slow.slowMultiplier = Mathf.Max(0f, slowMultiplier);
        slow.rooted = rooted;

        // Root always overrides slow in movement.
        domain.SetMovementRootedAndSlow(slow.rooted, slow.slowMultiplier);
    }

    public void ApplyInevitable(
        string abilityId,
        CombatStatsSnapshot attackerSnapshot,
        GameObject damageSource,
        float baseDamage,
        float baseDelaySeconds,
        float internalDamageCooldownSeconds)
    {
        if (domain == null && subdomain == null)
            return;

        this.damageSource = damageSource;

        var defenderSnapshot = GetDefenderSnapshot();

        // In your rules, statusResistance reduces debuff duration; inevitable damage itself is not reduced by damageReduction
        // and does not receive the unique-damage multiplier.
        float durationMultiplier = DurationMultiplierFromStatusResistance(defenderSnapshot.statusResistance);
        float effectiveDelay = Mathf.Max(0f, baseDelaySeconds * durationMultiplier);

        // Render once at application time.
        var (hpOnce, shieldOnce) = ResolveHpShieldDamage(
            attackerSnapshot,
            defenderSnapshot,
            renderedTotalDamage: baseDamage,
            isPhysicalDamage: false);

        if (inevitable == null)
            inevitable = new InevitableDelayedDamageRuntime();

        inevitable.hpOnceDamage = hpOnce;
        inevitable.shieldOnceDamage = shieldOnce;
        inevitable.internalDamageCooldownSeconds = internalDamageCooldownSeconds;
        inevitable.damageApplied = false; // will be re-applied at the next allowed first tick if rescheduled

        bool canScheduleFirstDamage = CanApplyAbilityDamageNow(inevitable, abilityId, internalDamageCooldownSeconds);
        if (canScheduleFirstDamage)
        {
            // Reschedule the first (delayed) damage from this ability application.
            inevitable.remainingDuration = effectiveDelay;
            inevitable.nextDamageTime = Time.time + effectiveDelay;
            inevitable.lastDamageAppliedByAbilityId[abilityId] = Time.time;
        }
        else
        {
            // ICD blocks "first tick" damage from this reapply:
            // refresh duration but do not reset the pending damage time.
            // If there was no pending schedule, arm it for the next allowed time.
            inevitable.remainingDuration = effectiveDelay;
            if (inevitable.nextDamageTime <= Time.time)
                inevitable.nextDamageTime = Time.time + effectiveDelay;

            // Prevent premature expiration if effectiveDelay shrinks due to changed statusResistance.
            if (inevitable.nextDamageTime > Time.time)
                inevitable.remainingDuration = Mathf.Max(inevitable.remainingDuration, inevitable.nextDamageTime - Time.time);
        }
    }

    private void TickAffliction()
    {
        if (affliction == null)
            return;

        affliction.remainingDuration -= Time.deltaTime;
        if (affliction.remainingDuration <= 0f)
        {
            affliction = null;
            return;
        }

        if (!affliction.tickingArmed)
            return;

        while (Time.time >= affliction.nextTickTime)
        {
            // Periodic ticks are already spaced by tickInterval; the ICD applies to the immediate "first tick"
            // on reapply, which is handled in ApplyAffliction().
            ApplyTickDamage();
            affliction.nextTickTime += affliction.tickInterval;
        }
    }

    private void TickInevitable()
    {
        if (inevitable == null)
            return;

        inevitable.remainingDuration -= Time.deltaTime;
        if (inevitable.remainingDuration <= 0f)
        {
            inevitable = null;
            return;
        }

        if (!inevitable.damageApplied && Time.time >= inevitable.nextDamageTime)
        {
            ApplyDamageToHost(inevitable.hpOnceDamage, inevitable.shieldOnceDamage);
            inevitable.damageApplied = true;

            // Inevitable is a one-shot first tick; expire immediately after it fires.
            inevitable.remainingDuration = 0f;
            inevitable = null;
        }
    }

    private void TickSlow()
    {
        if (slow == null)
            return;

        slow.remainingDuration -= Time.deltaTime;
        if (slow.remainingDuration <= 0f)
        {
            slow = null;
            domain.SetMovementRootedAndSlow(false, 1f);
            return;
        }

        // Keep movement modifiers in sync (duration refresh may have changed values).
        domain.SetMovementRootedAndSlow(slow.rooted, slow.slowMultiplier);
    }

    private void ApplyTickDamage()
    {
        if (affliction == null)
            return;
        ApplyDamageToHost(affliction.hpTickDamage, affliction.shieldTickDamage);
    }

    private CombatStatsSnapshot GetDefenderSnapshot()
    {
        if (domain != null)
            return domain.GetCombatStatsSnapshot();
        if (subdomain != null)
            return CombatStatsSnapshot.FromSubdomain(subdomain);
        return default;
    }

    private void ApplyDamageToHost(float hpDamage, float shieldDamage)
    {
        float mag = Mathf.Abs(hpDamage) + Mathf.Abs(shieldDamage);

        if (domain != null)
        {
            domain.TakeDamage(hpDamage, shieldDamage);
            if (mag > 0.0001f && damageSource != null)
            {
                var sync = damageSource.GetComponent<SyncPlayerStats>();
                sync?.ServerRegisterCombatActivity();
            }
            return;
        }

        if (subdomain != null)
        {
            var src = damageSource != null ? damageSource : gameObject;
            subdomain.TakeDamage(hpDamage, shieldDamage, src);
            if (mag > 0.0001f && damageSource != null)
            {
                var sync = damageSource.GetComponent<SyncPlayerStats>();
                sync?.ServerRegisterCombatActivity();
            }
        }
    }

    private static bool CanApplyAbilityDamageNow(AfflictionDotRuntime runtime, string abilityId, float icdSeconds)
    {
        if (runtime == null)
            return true;

        if (!runtime.lastDamageAppliedByAbilityId.TryGetValue(abilityId, out var lastTime))
            return true;

        return Time.time >= lastTime + icdSeconds;
    }

    private static bool CanApplyAbilityDamageNow(InevitableDelayedDamageRuntime runtime, string abilityId, float icdSeconds)
    {
        if (runtime == null)
            return true;

        if (!runtime.lastDamageAppliedByAbilityId.TryGetValue(abilityId, out var lastTime))
            return true;

        return Time.time >= lastTime + icdSeconds;
    }

    private static void MarkAbilityDamageApplied(AfflictionDotRuntime runtime, string abilityId)
    {
        if (runtime == null)
            return;
        runtime.lastDamageAppliedByAbilityId[abilityId] = Time.time;
    }

    private static float DurationMultiplierFromStatusResistance(float statusResistancePoints)
    {
        // Each 100 statusResistance => reduces duration by 2% => 0.02 per 100 => points * 0.0002
        float mult = 1f - statusResistancePoints * 0.0002f;
        return Mathf.Clamp(mult, 0f, 1f);
    }

    private static float UniqueDamageMultiplierFromStatusResistance(float statusResistancePoints)
    {
        // Each 100 statusResistance => reduces unique damage types by 1% => points * 0.0001
        float mult = 1f - statusResistancePoints * 0.0001f;
        return Mathf.Clamp(mult, 0f, 1f);
    }

    private static (float hp, float shield) ResolveHpShieldDamage(
        CombatStatsSnapshot attacker,
        CombatStatsSnapshot defender,
        float renderedTotalDamage,
        bool isPhysicalDamage)
    {
        if (renderedTotalDamage < 0f)
            renderedTotalDamage = 0f;

        // Flat damageReduction only affects Physical in your rules.
        float damageAfterFlatReduction = renderedTotalDamage;
        if (isPhysicalDamage)
        {
            float reductionMultiplier = 1f - (defender.damageReduction / 100f) * 0.01f;
            damageAfterFlatReduction = renderedTotalDamage * Mathf.Clamp(reductionMultiplier, 0f, 1f);
        }

        float denom = attacker.penetration + defender.integrity;
        float hpFraction;
        if (denom <= 0.0001f)
            hpFraction = 0.1f; // fallback to current behavior: 10% HP / 90% shield
        else
            hpFraction = attacker.penetration / denom;

        hpFraction = Mathf.Clamp01(hpFraction);
        float shieldFraction = 1f - hpFraction;

        return (damageAfterFlatReduction * hpFraction, damageAfterFlatReduction * shieldFraction);
    }
}

