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

    /// <summary>Server-authoritative: true while this player has dealt or taken damage in the last <see cref="CombatActivityWindowSeconds"/> (for no-logout-in-combat, UI, etc.).</summary>
    [SyncVar]
    public bool inCombat;

    /// <summary>How long after the last deal/take damage event <see cref="inCombat"/> stays true. See <see cref="CombatActivityRules"/>.</summary>
    public const float CombatActivityWindowSeconds = CombatActivityRules.InCombatWindowSeconds;

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

    /// <summary>
    /// Server: combat snapshot from authoritative <see cref="PlayerStatsManager"/> on this object.
    /// Client: local <see cref="PlayerStats"/> for UI only — damage/attacks use server state.
    /// </summary>
    public CombatStatsSnapshot GetCombatStatsSnapshot()
    {
        var psm = GetComponent<PlayerStatsManager>();
        return psm != null ? CombatStatsSnapshot.From(psm.playerStats) : default;
    }

    private float serverLastCombatActivityTime = -999f;

    /// <summary>Server only: call when this player deals or takes non-zero damage.</summary>
    public void ServerRegisterCombatActivity()
    {
        if (!isServer)
            return;
        serverLastCombatActivityTime = Time.time;
        if (!inCombat)
            inCombat = true;
    }

    private void Update()
    {
        if (!isServer || !inCombat)
            return;
        if (Time.time - serverLastCombatActivityTime > CombatActivityRules.InCombatWindowSeconds)
            inCombat = false;
    }
}
