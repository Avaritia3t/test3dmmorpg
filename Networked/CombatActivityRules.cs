/// <summary>
/// Single place for server-authoritative "in combat" semantics (see <see cref="SyncPlayerStats.inCombat"/>).
/// <para>
/// <b>Rule:</b> <see cref="SyncPlayerStats.inCombat"/> is true on the server if this player has
/// <b>dealt or taken non-zero HP/shield damage</b> within the last <see cref="InCombatWindowSeconds"/>.
/// It clears after that window with no further qualifying events (server <c>Update</c> on <see cref="SyncPlayerStats"/>).
/// </para>
/// <para>
/// <b>Code paths that register activity</b> (keep in sync when adding damage):
/// <list type="bullet">
/// <item><see cref="NetworkedDomainController.TakeDamage"/> — victim (player).</item>
/// <item><see cref="NetworkedAttackHandlerController"/> — attacker when damage is applied to domain/subdomain.</item>
/// <item><see cref="NetworkedStatusEffectController"/> DoT / ability damage — <c>damageSource</c> (attacker) when damage is applied.</item>
/// </list>
/// </para>
/// <para>
/// Intended for: logout restrictions, UI, buff rules — always read <see cref="SyncPlayerStats.inCombat"/> on server
/// or rely on SyncVar on clients; do not infer combat from animations alone.
/// </para>
/// </summary>
public static class CombatActivityRules
{
    /// <summary>Seconds after the last qualifying damage event before <see cref="SyncPlayerStats.inCombat"/> becomes false.</summary>
    public const float InCombatWindowSeconds = 5f;
}
