using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-authored ability: id, range, cooldown, and ordered effect list.
/// Create via Assets → Create → MMORPG → Ability Definition.
/// Wire into <see cref="NetworkedAbilityExecutor"/> on the player prefab.
/// </summary>
[CreateAssetMenu(menuName = "MMORPG/Ability Definition", fileName = "NewAbility")]
public class AbilityDefinitionSO : ScriptableObject
{
    [Tooltip("Stable id used by CmdTryCastAbility and cooldown keys.")]
    public string AbilityId = "Ability_Example";

    public string DisplayName = "Example";

    [Tooltip("Max distance from caster to target (meters).")]
    public float CastRangeMeters = 200f;

    [Tooltip("Server-side cooldown between successful casts of this ability.")]
    public float CooldownSeconds = 5f;

    [Tooltip("Effects applied in order when the ability resolves on the server.")]
    public List<AbilityEffectDefinition> Effects = new List<AbilityEffectDefinition>();
}
