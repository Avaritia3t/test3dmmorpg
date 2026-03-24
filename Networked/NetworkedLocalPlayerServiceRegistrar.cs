using Mirror;
using UnityEngine;

/// <summary>
/// Registers this player's services on <see cref="GameBootstrap.Locator"/> for the <b>local player only</b>.
/// Prefers <see cref="NetworkedPlayerEquipment"/> over <see cref="PlayerEquipmentManager"/> for <see cref="IPlayerEquipmentService"/> when both exist.
/// Add to the player prefab next to <see cref="NetworkIdentity"/>.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkedLocalPlayerServiceRegistrar : MonoBehaviour
{
    private bool registeredStats;
    private bool registeredEquipment;
    private bool registeredInventory;
    private bool didRegister;

    private void Start()
    {
        var ni = GetComponent<NetworkIdentity>();
        if (ni == null || !ni.isLocalPlayer)
            return;

        var locator = GameBootstrap.Locator;
        if (locator == null)
        {
            Debug.LogWarning("[NetworkedLocalPlayerServiceRegistrar] GameBootstrap.Locator is null.");
            return;
        }

        var stats = GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            locator.Register<IPlayerStatsService>(stats);
            registeredStats = true;
        }

        var netEquipment = GetComponent<NetworkedPlayerEquipment>();
        if (netEquipment != null)
        {
            locator.Register<IPlayerEquipmentService>(netEquipment);
            registeredEquipment = true;
        }
        else
        {
            var equipment = GetComponent<PlayerEquipmentManager>();
            if (equipment != null)
            {
                locator.Register<IPlayerEquipmentService>(equipment);
                registeredEquipment = true;
            }
        }

        var inv = GetComponent<NetworkedPlayerInventory>();
        if (inv != null)
        {
            locator.Register<IInventoryService>(inv);
            registeredInventory = true;
        }

        didRegister = registeredStats || registeredEquipment || registeredInventory;
    }

    private void OnDestroy()
    {
        if (!didRegister)
            return;

        var locator = GameBootstrap.Locator;
        if (locator == null)
            return;

        if (registeredStats)
            locator.Unregister<IPlayerStatsService>();
        if (registeredEquipment)
            locator.Unregister<IPlayerEquipmentService>();
        if (registeredInventory)
            locator.Unregister<IInventoryService>();
    }
}
