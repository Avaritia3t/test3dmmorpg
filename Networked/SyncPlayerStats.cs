using UnityEngine;
using Mirror;

/// <summary>
/// Server-authoritative sync of HP/shield for the player. Server updates when applying damage/regen; clients read for UI.
/// Required: on player prefab, same GameObject as NetworkIdentity and NetworkedDomainController.
/// </summary>
public class SyncPlayerStats : NetworkBehaviour
{
    [SyncVar]
    public float currentHP;

    [SyncVar]
    public float currentShield;

    [SyncVar]
    public float baseHP;

    [SyncVar]
    public float baseShield;

    /// <summary>Server-authoritative move speed (map buffs, combat effects). Clients apply this to NavMeshAgent.</summary>
    [SyncVar]
    public float syncMoveSpeed;

    /// <summary>Server: set authoritative move speed (computed from server-side stats).</summary>
    public void ServerSetMoveSpeed(float value)
    {
        if (!isServer) return;
        syncMoveSpeed = Mathf.Max(0f, value);
    }

    /// <summary>Server: initialize from a PlayerStats snapshot (e.g. on spawn).</summary>
    public void ServerInitFrom(PlayerStats stats)
    {
        if (!isServer) return;
        currentHP = stats.currentHP;
        currentShield = stats.currentShield;
        baseHP = stats.baseHP;
        baseShield = stats.baseShield;
    }

    /// <summary>Server: set current HP (e.g. after damage or regen).</summary>
    public void ServerSetCurrentHP(float value)
    {
        if (!isServer) return;
        currentHP = Mathf.Clamp(value, 0f, baseHP);
    }

    /// <summary>Server: set current shield (e.g. after damage or regen).</summary>
    public void ServerSetCurrentShield(float value)
    {
        if (!isServer) return;
        currentShield = Mathf.Clamp(value, 0f, baseShield);
    }

    // Server-only: combat stats sent by client so server uses up-to-date modifiers (equipment, map buffs)
    private CombatStatsSnapshot serverCombatStats;

    /// <summary>Client (local player): send current combat stats to server so attacks use correct modifiers.</summary>
    public void SendCombatStats(CombatStatsSnapshot snapshot)
    {
        if (isLocalPlayer)
            CmdSetCombatStats(snapshot);
    }

    [Command]
    private void CmdSetCombatStats(CombatStatsSnapshot snapshot)
    {
        serverCombatStats = snapshot;
    }

    /// <summary>Server: get combat stats for this player (from client snapshot, or default).</summary>
    public CombatStatsSnapshot GetCombatStatsSnapshot()
    {
        return serverCombatStats;
    }
}
