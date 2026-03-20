using System.Collections;
using UnityEngine;

/// <summary>
/// Networked attack handler: only NetworkedDomainController and NetworkedSubdomainController as attacker/target.
/// Use with INetworkedAttackHandlerPool. Leave Combat/AttackHandlerV2 unchanged for the local stack.
/// Required: on pooled attack-handler prefab (instantiated by NetworkedAttackHandlerPool). No Mirror component required.
/// </summary>
public class NetworkedAttackHandlerController : MonoBehaviour
{
    public float attackSpeed;
    public float attackDamage;
    public float attackRange;
    public float attackTimer;
    public GameObject attackTarget;
    public bool isAttacking;

    private IPlayerStatsService playerStatsService;
    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();

    public void Initialize(GameObject attacker, GameObject target)
    {
        var networkedPlayer = attacker.GetComponent<NetworkedDomainController>();
        var networkedNpc = attacker.GetComponent<NetworkedSubdomainController>();

        if (networkedPlayer != null)
        {
            var snap = networkedPlayer.GetCombatStatsSnapshot();
            attackSpeed = snap.attackSpeed;
            attackDamage = snap.attackDamage;
            attackRange = snap.attackRange;
        }
        else if (networkedNpc != null)
        {
            attackSpeed = networkedNpc.SubdomainAttackSpeed;
            attackDamage = networkedNpc.SubdomainDamage;
            attackRange = networkedNpc.attackRange;
            // Debug.Log("[NetworkedAttackHandlerController] NPC Attack Range: " + attackRange);
        }

        attackTarget = target;
        isAttacking = true;
        attackTimer = 0f;
    }

    public void ResetHandler()
    {
        attackSpeed = 0f;
        attackDamage = 0f;
        attackRange = 0f;
        attackTimer = 0f;
        attackTarget = null;
        isAttacking = false;
        // Debug.Log("[NetworkedAttackHandlerController] Handler reset.");
    }

    public void AttemptAttack(GameObject attacker)
    {
        if (attackTarget == null)
        {
            Debug.LogError("[NetworkedAttackHandlerController] AttemptAttack failed: attackTarget is null");
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= 1f / attackSpeed)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(attackDamage);

            if (attackTarget != null)
            {
                ApplyDamage(attackTarget, attacker, hpDamage, shieldDamage);
                ApplyEffects(attackTarget, attacker);
            }
            else
            {
                Debug.LogError("[NetworkedAttackHandlerController] AttemptAttack failed: attackTarget became null before damage was applied");
            }

            attackTimer = 0f;
        }
    }

    private void ApplyDamage(GameObject target, GameObject attacker, float hpDamage, float shieldDamage)
    {
        var networkedDomain = target.GetComponent<NetworkedDomainController>();
        if (networkedDomain != null)
        {
            networkedDomain.TakeDamage(hpDamage, shieldDamage);
            // Debug.Log("[NetworkedAttackHandlerController] Applied damage to " + target.name);
            return;
        }

        var networkedSubdomain = target.GetComponent<NetworkedSubdomainController>();
        if (networkedSubdomain != null)
        {
            networkedSubdomain.TakeDamage(hpDamage, shieldDamage, attacker);
            // Debug.Log("[NetworkedAttackHandlerController] Applied damage to " + target.name);
            return;
        }

        Debug.LogError("[NetworkedAttackHandlerController] ApplyDamage failed: Target does not have NetworkedDomainController or NetworkedSubdomainController");
    }

    private IEnumerator ApplyAffliction(GameObject target, float afflictionDamage, float duration)
    {
        float interval = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            var networkedDomain = target.GetComponent<NetworkedDomainController>();
            if (networkedDomain != null)
                networkedDomain.TakeDamage(afflictionDamage, 0);
            else if (target.GetComponent<NetworkedSubdomainController>() != null)
                target.GetComponent<NetworkedSubdomainController>().TakeDamage(afflictionDamage, 0, gameObject);
            else
                Debug.LogError("[NetworkedAttackHandlerController] ApplyAffliction failed: Target does not have a known controller");

            elapsed += interval;
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator ApplyInevitableDamage(GameObject target, float inevitableDamage, float delay)
    {
        yield return new WaitForSeconds(delay);

        var networkedDomain = target.GetComponent<NetworkedDomainController>();
        if (networkedDomain != null)
            networkedDomain.TakeDamage(inevitableDamage, 0);
        else if (target.GetComponent<NetworkedSubdomainController>() != null)
            target.GetComponent<NetworkedSubdomainController>().TakeDamage(inevitableDamage, 0, gameObject);
        else
            Debug.LogError("[NetworkedAttackHandlerController] ApplyInevitableDamage failed: Target does not have a known controller");
        // Debug.Log("[NetworkedAttackHandlerController] Inevitable Damage Applied!");
    }

    private void ApplyEffects(GameObject target, GameObject attacker)
    {
        CombatStatsSnapshot attackerStats = GetAttackerCombatStats(attacker);
        if (!attackerStats.IsValid) return;

        if (Random.value < attackerStats.criticalChance)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(attackDamage * attackerStats.criticalDamage);
            ApplyDamage(target, attacker, hpDamage, shieldDamage);
        }

        if (Random.value < attackerStats.afflictionChance)
        {
            var status = target.GetComponent<NetworkedStatusEffectController>();
            if (status != null)
            {
                // Affliction DoT:
                // - duration uptime refreshes to full
                // - immediate "first tick" damage is gated by ICD
                float baseDuration = 10f;
                float tickInterval = 2f;
                float internalDamageCooldown = tickInterval; // First pass: ICD equals tick interval.

                status.ApplyAffliction(
                    abilityId: "Affliction",
                    attackerSnapshot: attackerStats,
                    damageSource: attacker,
                    baseTickDamage: attackerStats.afflictionDamage,
                    baseDurationSeconds: baseDuration,
                    tickIntervalSeconds: tickInterval,
                    internalDamageCooldownSeconds: internalDamageCooldown);
            }
            else
            {
                // Fallback for targets that don't have NetworkedStatusEffectController yet.
                StartCoroutine(ApplyAffliction(target, attackerStats.afflictionDamage, 10f));
            }
        }

        if (Random.value < attackerStats.etherealChance)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(attackerStats.etherealDamage);
            ApplyDamage(target, attacker, hpDamage, shieldDamage);
        }

        if (Random.value < attackerStats.demonicChance)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(attackerStats.demonicDamage);
            ApplyDamage(target, attacker, hpDamage, shieldDamage);
        }

        if (Random.value < attackerStats.inevitableChance)
        {
            var status = target.GetComponent<NetworkedStatusEffectController>();
            if (status != null)
            {
                float delay = 3f;
                float internalDamageCooldown = delay; // First pass.

                status.ApplyInevitable(
                    abilityId: "Inevitable",
                    attackerSnapshot: attackerStats,
                    damageSource: attacker,
                    baseDamage: attackerStats.inevitableDamage,
                    baseDelaySeconds: delay,
                    internalDamageCooldownSeconds: internalDamageCooldown);
            }
            else
            {
                // Fallback for targets that don't have the new controller yet.
                StartCoroutine(ApplyInevitableDamage(target, attackerStats.inevitableDamage, 3f));
            }
        }
    }

    private static CombatStatsSnapshot GetAttackerCombatStats(GameObject attacker)
    {
        if (attacker == null) return default;
        var domain = attacker.GetComponent<NetworkedDomainController>();
        return domain != null ? domain.GetCombatStatsSnapshot() : default;
    }

    public (float, float) SetOutgoingDamage(float damage)
    {
        float shieldDamage = damage * 0.9f;
        float hpDamage = damage * 0.1f;
        return (hpDamage, shieldDamage);
    }

    public void SetTarget(GameObject target)
    {
        if (target != null)
        {
            attackTarget = target;
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
            attackTarget = null;
        }
    }
}
