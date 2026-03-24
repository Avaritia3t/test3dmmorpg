using UnityEngine;
using Mirror;

/// <summary>
/// Handles player combat: target and attack from client Commands; attack execution server-only.
/// Required: on player prefab, same GameObject as NetworkIdentity and NetworkedDomainController.
/// <para>Attack cadence uses the same <see cref="CombatStatsSnapshot"/> source as <see cref="NetworkedAttackHandlerController"/> (server <see cref="PlayerStatsManager"/>).</para>
/// </summary>
public class NetworkedPlayerCombatHelperController : NetworkBehaviour
{
    private float lastAttackTime;
    private NetworkedAttackHandlerController attackHandler;

    private IPlayerStatsService playerStatsService;
    private INetworkedAttackHandlerPool networkedAttackHandlerPool;
    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private INetworkedAttackHandlerPool AttackHandlerPool => networkedAttackHandlerPool ??= GameBootstrap.Locator?.Get<INetworkedAttackHandlerPool>();

    [SyncVar]
    private bool isAttacking;
    private GameObject serverTarget;

    /// <summary>Last target the local client selected (for ability intents). Server uses <see cref="serverTarget"/>.</summary>
    private uint localTargetNetId;

    public bool IsAttacking => isAttacking;

    public uint LocalTargetNetId => localTargetNetId;

    /// <summary>
    /// Called by the domain controller when the user clicks. Client sends target to server via Command.
    /// </summary>
    public bool TrySetTargetFromHover(GameObject hoveredObject)
    {
        if (hoveredObject == null) return false;
        if (!IsValidTarget(hoveredObject)) return false;

        var targetIdentity = hoveredObject.GetComponent<NetworkIdentity>();
        if (targetIdentity != null)
        {
            localTargetNetId = targetIdentity.netId;
            CmdSetTarget(targetIdentity);
            return true;
        }
        localTargetNetId = 0;
        serverTarget = hoveredObject;
        return true;
    }

    /// <summary>
    /// Toggles attacking. Server flips <see cref="isAttacking"/> (SyncVar) — no optimistic client flip.
    /// </summary>
    public void ToggleAttacking()
    {
        if (!NetworkClient.active && !NetworkServer.active)
        {
            isAttacking = !isAttacking;
            if (!isAttacking)
                ServerStopAttack();
            return;
        }

        if (!isLocalPlayer)
            return;

        CmdToggleAttacking();
    }

    [Command]
    private void CmdSetTarget(NetworkIdentity target)
    {
        serverTarget = target != null ? target.gameObject : null;
    }

    [Command]
    private void CmdToggleAttacking()
    {
        isAttacking = !isAttacking;
        if (!isAttacking)
            ServerStopAttack();
    }

    /// <summary>
    /// Called every frame. Server runs attack cycle; in single-player (no Mirror) runs locally.
    /// </summary>
    public void Tick()
    {
        if (!isAttacking || serverTarget == null) return;
        if (isClient && !isServer) return;
        TryAutoAttackInRange();
    }

    private bool IsValidTarget(GameObject obj)
    {
        return obj.GetComponent<NetworkedSubdomainController>() != null;
    }

    private void TryAutoAttackInRange()
    {
        if (serverTarget == null)
        {
            isAttacking = false;
            return;
        }
        if (PlayerStatsService == null || GetCurrentPlayerHP() <= 0f)
        {
            isAttacking = false;
            return;
        }

        var subdomain = serverTarget.GetComponent<NetworkedSubdomainController>();
        if (subdomain == null || subdomain.currentHP <= 0)
        {
            isAttacking = false;
            serverTarget = null;
            return;
        }

        if (CheckAttackInterval())
            PerformAttack();
    }

    private float GetCurrentPlayerHP()
    {
        var sync = GetComponent<SyncPlayerStats>();
        if (sync != null) return sync.currentHP;
        var stats = GetComponent<PlayerStatsManager>()?.playerStats;
        return stats != null ? stats.currentHP : 0f;
    }

    private bool CheckAttackInterval()
    {
        // Server-only path (Tick already skips pure clients). Match NetworkedAttackHandlerController / GetCombatStatsSnapshot.
        float attackSpeed = 0f;
        var domain = GetComponent<NetworkedDomainController>();
        if (domain != null)
        {
            var snap = domain.GetCombatStatsSnapshot();
            if (snap.IsValid && snap.attackSpeed > 0f)
                attackSpeed = snap.attackSpeed;
        }
        if (attackSpeed <= 0f)
        {
            var stats = GetComponent<PlayerStatsManager>()?.playerStats;
            if (stats == null || stats.attackSpeed <= 0f) return false;
            attackSpeed = stats.attackSpeed;
        }

        float waitTime = 151.67f / attackSpeed - 0.0167f;
        return Time.time - lastAttackTime >= waitTime;
    }

    private void PerformAttack()
    {
        if (serverTarget == null) return;
        if (AttackHandlerPool == null) return;

        if (attackHandler == null)
        {
            attackHandler = AttackHandlerPool.RequestHandler();
            if (attackHandler == null) return;
        }

        attackHandler.Initialize(gameObject, serverTarget);
        attackHandler.AttemptAttack(gameObject);

        lastAttackTime = Time.time;
        attackHandler.isAttacking = false;
        AttackHandlerPool.ReturnHandler(attackHandler);
        attackHandler = null;
    }

    private void ServerStopAttack()
    {
        if (attackHandler != null)
        {
            if (AttackHandlerPool != null)
            {
                attackHandler.isAttacking = false;
                AttackHandlerPool.ReturnHandler(attackHandler);
            }
            attackHandler = null;
        }
    }
}
