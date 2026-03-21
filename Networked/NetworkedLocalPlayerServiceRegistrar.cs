using Mirror;
using UnityEngine;

/// <summary>
/// Registers this player's <see cref="PlayerStatsManager"/>, <see cref="PlayerEquipmentManager"/>, and <see cref="NetworkedPlayerInventory"/> on <see cref="GameBootstrap.Locator"/> for the <b>local player only</b>.
/// (<see cref="NetworkedPlayerEquipment"/> is optional and currently compiled out — see that file when enabling server-authoritative equip.)
/// Add to the player prefab next to <see cref="NetworkIdentity"/>.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkedLocalPlayerServiceRegistrar : MonoBehaviour
{
    private bool registeredStats;
    private bool registeredEquipment;
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

        var equipment = GetComponent<PlayerEquipmentManager>();
        if (equipment != null)
        {
            locator.Register<IPlayerEquipmentService>(equipment);
            registeredEquipment = true;
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
