using UnityEngine;

/// <summary>
/// Handles player combat: target selection (from hovered object), attack toggle, and attack cycle.
/// Controller passes screen-point hit (hovered object); this component validates, sets target, and runs attacks.
/// Intended to run on the player; server authority for combat can be applied later.
/// </summary>
public class NetworkedPlayerCombatHelperController : MonoBehaviour
{
    private GameObject selectedTarget;
    private bool isAttacking;
    private float lastAttackTime;
    private NetworkedAttackHandlerController attackHandler;

    private IPlayerStatsService playerStatsService;
    private INetworkedAttackHandlerPool networkedAttackHandlerPool;
    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private INetworkedAttackHandlerPool AttackHandlerPool => networkedAttackHandlerPool ??= GameBootstrap.Locator?.Get<INetworkedAttackHandlerPool>();

    /// <summary>
    /// Called by the domain controller when the user clicks: pass the hovered object (e.g. from raycast).
    /// If attacking is allowed and the object is a valid target, sets it as current target and establishes attacker/target relationship.
    /// Does not auto-start the attack cycle; use ToggleAttacking() or separate flow for that.
    /// </summary>
    /// <param name="hoveredObject">Object under the cursor (e.g. hit.collider.gameObject). Can be null.</param>
    /// <returns>True if the object was accepted as a valid target.</returns>
    public bool TrySetTargetFromHover(GameObject hoveredObject)
    {
        if (hoveredObject == null) return false;
        if (!IsValidTarget(hoveredObject)) return false;

        selectedTarget = hoveredObject;
        // Debug.Log("[NetworkedPlayerCombatHelperController] Target set from hover: " + hoveredObject.name);
        return true;
    }

    /// <summary>
    /// Toggles attacking on/off. When turning off, returns the attack handler to the pool.
    /// </summary>
    public void ToggleAttacking()
    {
        isAttacking = !isAttacking;
        // Debug.Log("[NetworkedPlayerCombatHelperController] CTRL pressed. Attacking state is now: " + isAttacking);
        if (!isAttacking)
            StopAttack();
    }

    /// <summary>
    /// Called every frame. Runs the attack cycle when attacking and target is valid.
    /// </summary>
    public void Tick()
    {
        if (!isAttacking || selectedTarget == null) return;
        TryAutoAttackInRange();
    }

    /// <summary>
    /// Whether the object can be targeted (e.g. has NetworkedSubdomainController). Extend for other target types.
    /// </summary>
    private bool IsValidTarget(GameObject obj)
    {
        return obj.GetComponent<NetworkedSubdomainController>() != null;
    }

    private void TryAutoAttackInRange()
    {
        if (selectedTarget == null || PlayerStatsService == null || PlayerStatsService.playerStats.currentHP <= 0)
        {
            // Debug.Log("[NetworkedPlayerCombatHelperController] No valid target or player is dead. Stopping attack.");
            isAttacking = false;
            return;
        }

        var subdomain = selectedTarget.GetComponent<NetworkedSubdomainController>();
        if (subdomain == null || subdomain.currentHP <= 0)
        {
            // Debug.Log("[NetworkedPlayerCombatHelperController] Target HP is zero or no longer valid. Stopping attack.");
            isAttacking = false;
            selectedTarget = null;
            return;
        }

        if (CheckAttackInterval())
            PerformAttack();
    }

    private bool CheckAttackInterval()
    {
        if (PlayerStatsService == null) return false;

        float attackSpeed = PlayerStatsService.playerStats.attackSpeed;
        // TODO: replace with proper formula when available
        float waitTime = 151.67f / attackSpeed - 0.0167f;

        if (Time.time - lastAttackTime >= waitTime)
        {
            // Debug.Log("[NetworkedPlayerCombatHelperController] Attack interval elapsed. Ready to attack.");
            return true;
        }
        // Debug.Log("[NetworkedPlayerCombatHelperController] Not enough time since last attack.");
        return false;
    }

    private void PerformAttack()
    {
        if (selectedTarget == null)
        {
            Debug.LogError("[NetworkedPlayerCombatHelperController] PerformAttack: No target.");
            return;
        }

        if (AttackHandlerPool == null)
        {
            Debug.LogError("[NetworkedPlayerCombatHelperController] INetworkedAttackHandlerPool not found. Register NetworkedAttackHandlerPool in the locator.");
            return;
        }

        if (attackHandler == null)
        {
            // Debug.Log("[NetworkedPlayerCombatHelperController] Requesting a new AttackHandler.");
            attackHandler = AttackHandlerPool.RequestHandler();
            if (attackHandler == null)
            {
                Debug.LogError("[NetworkedPlayerCombatHelperController] Failed to get AttackHandler from the pool.");
                return;
            }
        }

        GameObject attacker = gameObject;
        attackHandler.Initialize(attacker, selectedTarget);
        // Debug.Log("[NetworkedPlayerCombatHelperController] Attempting attack on target.");
        attackHandler.AttemptAttack(attacker);

        lastAttackTime = Time.time;
        attackHandler.isAttacking = false;
        AttackHandlerPool.ReturnHandler(attackHandler);
        attackHandler = null;
        // Debug.Log("[NetworkedPlayerCombatHelperController] Attack completed. Handler returned to pool.");
    }

    private void StopAttack()
    {
        if (attackHandler != null)
        {
            if (AttackHandlerPool != null)
            {
                // Debug.Log("[NetworkedPlayerCombatHelperController] Returning AttackHandler to the pool.");
                attackHandler.isAttacking = false;
                AttackHandlerPool.ReturnHandler(attackHandler);
            }
            attackHandler = null;
        }
    }
}
