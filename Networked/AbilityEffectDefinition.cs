using UnityEngine;

/// <summary>
/// First-pass catalogue entry for a single server-applied status-effect component.
/// Abilities can own a list of these; each effect is applied to a selected target.
///
/// Notes:
/// - For DoT effects, repeated applications refresh debuff uptime to full, but "first tick" damage is ICD-gated
///   by <see cref="InternalDamageCooldownSeconds"/> (managed by NetworkedStatusEffectController).
/// - For movement control: rooted disables movement override; slow multiplies speed.
/// </summary>
[System.Serializable]
public class AbilityEffectDefinition
{
    public enum EffectType
    {
        AfflictionDoT,
        InevitableDelayedDamage,
        Slow,
        Root
    }

    [Tooltip("Unique id for the effect type (used for UI/debug).")]
    public string EffectId = "";

    public EffectType Type;

    [Tooltip("How far the ability can affect targets (optional for runtime; used by ability casting/targeting).")]
    public float EffectRangeMeters = 0f;

    [Header("Uptime (refreshes to full)")]
    [Tooltip("Debuff uptime (seconds). For delayed-damage effects, this can be used as a visual duration if needed.")]
    public float DurationSeconds = 0f;

    [Header("DoT (Affliction)")]
    public float DamageIntervalSeconds = 2f;

    [Header("DoT Damage Formula (Affliction)")]
    [Tooltip("Constant flat damage added to the damage formula (per tick).")]
    public float DoTFlatDamageConstantPerTick = 0f;
    [Tooltip("Optional: name of an attacker stat on CombatStatsSnapshot used as the flat component for DoT damage (per tick).")]
    public string DoTFlatDamageStatSourceNamePerTick = "";

    [Tooltip("Constant % component applied to the target base HP (per tick). Use percent points (e.g. 0.2 means 0.2%).")]
    public float DoTPercentOfTargetBaseHPConstantPerTick = 0f;
    [Tooltip("Optional: name of an attacker stat on CombatStatsSnapshot used as the % component (percent points) for DoT damage (per tick).")]
    public string DoTPercentOfTargetBaseHPStatSourceNamePerTick = "";

    [Tooltip("Multiplier applied after (flat + %component) for DoT damage (per tick).")]
    public float DoTDamageMultiplierPerTick = 1f;

    [Header("Delayed Damage (Inevitable)")]
    [Tooltip("Seconds from application until the delayed damage fires.")]
    public float DelaySeconds = 3f;

    [Header("Delayed Damage Formula (Inevitable)")]
    [Tooltip("Constant flat damage added to the damage formula (once).")]
    public float InevitableFlatDamageConstantOnce = 0f;
    [Tooltip("Optional: name of an attacker stat on CombatStatsSnapshot used as the flat component for inevitable damage (once).")]
    public string InevitableFlatDamageStatSourceNameOnce = "";

    [Tooltip("Constant % component applied to the target base HP (once). Use percent points (e.g. 0.2 means 0.2%).")]
    public float InevitablePercentOfTargetBaseHPConstantOnce = 0f;
    [Tooltip("Optional: name of an attacker stat on CombatStatsSnapshot used as the % component (percent points) for inevitable damage (once).")]
    public string InevitablePercentOfTargetBaseHPStatSourceNameOnce = "";

    [Tooltip("Multiplier applied after (flat + %component) for inevitable damage (once).")]
    public float InevitableDamageMultiplierOnce = 1f;

    [Header("Damage ICD (anti-reapply first tick)")]
    [Tooltip("Internal cooldown (seconds) controlling whether the 'first tick/first delayed hit' is allowed on reapply.")]
    public float InternalDamageCooldownSeconds = 2f;

    [Header("Movement Control")]
    [Tooltip("Slow multiplier applied to move speed while effect is active (e.g. 0.7 = -30%).")]
    public float SlowMultiplier = 1f;
    [Tooltip("If true, rooted disables movement rather than only slowing it.")]
    public bool Rooted = false;
}

