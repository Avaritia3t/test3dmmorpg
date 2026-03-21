using UnityEngine;

/// <summary>
/// Reserved for player-scoped services registered at bootstrap (e.g. offline defaults).
/// Networked per-player services like <see cref="IPlayerStatsService"/> are registered when the local player spawns
/// (<see cref="NetworkedLocalPlayerServiceRegistrar"/>).
/// </summary>
public static class PlayerStartup
{
    public static void Configure(ServiceLocator locator)
    {
        // IPlayerStatsService / IPlayerEquipmentService are registered by the local player's
        // NetworkedLocalPlayerServiceRegistrar when the local NetworkIdentity spawns (not FindObjectOfType).
    }
}
