using UnityEngine;
using Mirror;

/// <summary>
/// Server-authoritative sync of subdomain HP, shield, and state. Clients read for display (e.g. NPC health bar).
/// Required: on subdomain prefab, same GameObject as NetworkIdentity and NetworkedSubdomainController.
/// </summary>
public class SyncSubdomainState : NetworkBehaviour
{
    [SyncVar]
    public float currentHP;

    [SyncVar]
    public float currentShield;

    [SyncVar]
    public int currentStateIndex;

    public SubdomainV2.SubdomainState CurrentState
    {
        get => (SubdomainV2.SubdomainState)currentStateIndex;
        set => currentStateIndex = (int)value;
    }

    public void ServerSetHP(float value)
    {
        if (!NetworkServer.active) return;
        currentHP = Mathf.Clamp(value, 0f, float.MaxValue);
    }

    public void ServerSetShield(float value)
    {
        if (!NetworkServer.active) return;
        currentShield = Mathf.Clamp(value, 0f, float.MaxValue);
    }

    public void ServerSetState(SubdomainV2.SubdomainState state)
    {
        if (!NetworkServer.active) return;
        currentStateIndex = (int)state;
    }
}
