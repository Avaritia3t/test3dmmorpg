using UnityEngine;
using System.Collections;

public class AttackHandlerV2 : MonoBehaviour
{
    public float attackSpeed;
    public float attackDamage;
    public float attackRange;
    public float attackTimer = 0f;
    public GameObject attackTarget;
    public DomainControllerV3 playerController;
    public SubdomainV2 npcController;
    public bool isAttacking = false;

    private void Start()
    {
        // Initialize with player stats by default
        playerController = GameObject.Find("PlayerObject")?.GetComponent<DomainControllerV3>();
        if (playerController != null)
        {
            var playerStats = PlayerStatsManager.Instance.playerStats;
            attackSpeed = playerStats.attackSpeed;
            attackDamage = playerStats.currentDamage;
            attackRange = playerStats.attackRange;
        }
    }

    public void Initialize(GameObject attacker, GameObject target)
    {
        playerController = attacker.GetComponent<DomainControllerV3>();
        npcController = attacker.GetComponent<SubdomainV2>();

        if (playerController != null)
        {
            attackSpeed = PlayerStatsManager.Instance.playerStats.attackSpeed;
            attackDamage = PlayerStatsManager.Instance.playerStats.currentDamage;
            attackRange = PlayerStatsManager.Instance.playerStats.attackRange;
            Debug.Log($"[Initialize] Player Attack Range: {attackRange}");
        }
        else if (npcController != null)
        {
            attackSpeed = npcController.SubdomainAttackSpeed;
            attackDamage = npcController.SubdomainDamage;
            attackRange = npcController.attackRange;
            Debug.Log($"[Initialize] NPC Attack Range: {attackRange}");
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
        playerController = null;
        npcController = null;

        Debug.Log("[AttackHandler] Handler reset.");
    }

    public void AttemptAttack(GameObject attacker)
    {
        if (attackTarget == null)
        {
            Debug.LogError("[AttackHandler] AttemptAttack failed: attackTarget is null");
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
                Debug.LogError("[AttackHandler] AttemptAttack failed: attackTarget became null before damage was applied");
            }

            attackTimer = 0f;
        }
    }

    private void ApplyDamage(GameObject target, GameObject attacker, float hpDamage, float shieldDamage)
    {
        DomainControllerV3 domainController = target.GetComponent<DomainControllerV3>();
        if (domainController != null)
        {
            domainController.TakeDamage(hpDamage, shieldDamage);
            Debug.Log($"[AttackHandler] Applied {hpDamage} HP damage and {shieldDamage} shield damage to {target.name}");
        }
        else
        {
            SubdomainV2 subdomain = target.GetComponent<SubdomainV2>();
            if (subdomain != null)
            {
                subdomain.TakeDamage(hpDamage, shieldDamage, attacker);
                Debug.Log($"[AttackHandler] Applied {hpDamage} HP damage and {shieldDamage} shield damage to {target.name}");
            }
            else
            {
                Debug.LogError("[AttackHandler] ApplyDamage failed: Target does not have a DomainControllerV3 or SubdomainV2 component");
            }
        }
    }

    private IEnumerator ApplyAffliction(GameObject target, float afflictionDamage, float duration)
    {
        float interval = 2f; // Apply damage every 2 seconds
        float elapsed = 0f;

        while (elapsed < duration)
        {
            DomainControllerV3 domainController = target.GetComponent<DomainControllerV3>();
            if (domainController != null)
            {
                domainController.TakeDamage(afflictionDamage, 0);
            }
            else
            {
                SubdomainV2 subdomain = target.GetComponent<SubdomainV2>();
                if (subdomain != null)
                {
                    subdomain.TakeDamage(afflictionDamage, 0, gameObject);
                }
                else
                {
                    Debug.LogError("[AttackHandler] ApplyAffliction failed: Target does not have a DomainControllerV3 or SubdomainV2 component");
                }
            }

            elapsed += interval;
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator ApplyInevitableDamage(GameObject target, float inevitableDamage, float delay)
    {
        yield return new WaitForSeconds(delay);

        DomainControllerV3 domainController = target.GetComponent<DomainControllerV3>();
        if (domainController != null)
        {
            domainController.TakeDamage(inevitableDamage, 0);
        }
        else
        {
            SubdomainV2 subdomain = target.GetComponent<SubdomainV2>();
            if (subdomain != null)
            {
                subdomain.TakeDamage(inevitableDamage, 0, gameObject);
            }
            else
            {
                Debug.LogError("[AttackHandler] ApplyInevitableDamage failed: Target does not have a DomainControllerV3 or SubdomainV2 component");
            }
        }
        Debug.Log("[AttackHandler] Inevitable Damage Applied!");
    }

    private void ApplyEffects(GameObject target, GameObject attacker)
    {
        var playerStats = PlayerStatsManager.Instance.playerStats;

        // Apply Critical Hit
        if (Random.value < playerStats.criticalChance)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(attackDamage * playerStats.criticalDamage);
            ApplyDamage(target, attacker, hpDamage, shieldDamage);
            Debug.Log("[AttackHandler] Critical Hit!");
        }

        // Apply Affliction (Bleed) Effect
        if (Random.value < playerStats.afflictionChance)
        {
            StartCoroutine(ApplyAffliction(target, playerStats.afflictionDamage, 10f)); // Example duration
            Debug.Log("[AttackHandler] Affliction (Bleed) Effect Applied!");
        }

        // Apply Ethereal Damage
        if (Random.value < playerStats.etherealChance)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(playerStats.etherealDamage);
            ApplyDamage(target, attacker, hpDamage, shieldDamage);
            Debug.Log("[AttackHandler] Ethereal Damage Applied!");
        }

        // Apply Demonic Damage
        if (Random.value < playerStats.demonicChance)
        {
            var (hpDamage, shieldDamage) = SetOutgoingDamage(playerStats.demonicDamage);
            ApplyDamage(target, attacker, hpDamage, shieldDamage);
            Debug.Log("[AttackHandler] Demonic Damage Applied!");
        }

        // Apply Inevitable Damage
        if (Random.value < playerStats.inevitableChance)
        {
            StartCoroutine(ApplyInevitableDamage(target, playerStats.inevitableDamage, 3f)); // Apply after 3 seconds
        }
    }

    public (float, float) SetOutgoingDamage(float damage)
    {
        float shieldDamage = damage * 0.9f; // 90% of damage goes to shield
        float hpDamage = damage * 0.1f;     // 10% of damage goes to HP
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
