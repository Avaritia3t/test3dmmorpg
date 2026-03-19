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
}
