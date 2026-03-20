using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Local player sends ability intent; server validates cooldown/range and applies <see cref="AbilityEffectDefinition"/> via <see cref="AbilityEffectApplier"/>.
/// Required: on player prefab with NetworkIdentity, NetworkedDomainController, NetworkedPlayerCombatHelperController.
/// Assign <see cref="abilityCatalog"/> (ScriptableObjects) in the inspector. Keys 1–9 cast catalog slots 0–8.
/// </summary>
public class NetworkedAbilityExecutor : NetworkBehaviour
{
    [SerializeField]
    private AbilityDefinitionSO[] abilityCatalog = new AbilityDefinitionSO[0];

    private NetworkedDomainController domain;
    private NetworkedPlayerCombatHelperController combat;

    private readonly Dictionary<string, float> serverLastCastTime = new Dictionary<string, float>();

    private void Awake()
    {
        domain = GetComponent<NetworkedDomainController>();
        combat = GetComponent<NetworkedPlayerCombatHelperController>();
    }

    private void Update()
    {
        if (!isLocalPlayer || abilityCatalog == null || abilityCatalog.Length == 0)
            return;

        for (int i = 0; i < abilityCatalog.Length && i < 9; i++)
        {
            if (abilityCatalog[i] == null)
                continue;
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                CmdTryCastAbility(abilityCatalog[i].AbilityId, combat != null ? combat.LocalTargetNetId : 0u);
        }
    }

    [Command]
    private void CmdTryCastAbility(string abilityId, uint targetNetId)
    {
        if (string.IsNullOrEmpty(abilityId) || targetNetId == 0)
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out var targetIdentity))
            return;

        var def = FindAbility(abilityId);
        if (def == null || def.Effects == null || def.Effects.Count == 0)
            return;

        float now = Time.time;
        if (serverLastCastTime.TryGetValue(abilityId, out float last) && now - last < def.CooldownSeconds)
            return;

        GameObject targetGo = targetIdentity.gameObject;
        float dist = Vector3.Distance(transform.position, targetGo.transform.position);
        if (dist > def.CastRangeMeters)
            return;

        if (domain == null)
            return;

        var attackerSnap = domain.GetCombatStatsSnapshot();
        if (!attackerSnap.IsValid)
            return;

        serverLastCastTime[abilityId] = now;

        foreach (var effect in def.Effects)
        {
            if (effect == null)
                continue;
            AbilityEffectApplier.ApplyToTarget(targetGo, abilityId, attackerSnap, effect, gameObject);
        }
    }

    private AbilityDefinitionSO FindAbility(string abilityId)
    {
        if (abilityCatalog == null)
            return null;
        for (int i = 0; i < abilityCatalog.Length; i++)
        {
            var a = abilityCatalog[i];
            if (a != null && a.AbilityId == abilityId)
                return a;
        }
        return null;
    }
}
