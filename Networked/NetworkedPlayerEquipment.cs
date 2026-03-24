using Mirror;
using UnityEngine;

/// <summary>
/// Server-authoritative equipment: local player sends <see cref="Command"/>s; server applies
/// <see cref="PlayerEquipmentManager"/> and mutates <see cref="NetworkedPlayerInventory"/> on this player (not global locator).
/// Register as <see cref="IPlayerEquipmentService"/> via <see cref="NetworkedLocalPlayerServiceRegistrar"/> for the local player.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(PlayerEquipmentManager))]
[RequireComponent(typeof(NetworkedPlayerInventory))]
public class NetworkedPlayerEquipment : NetworkBehaviour, IPlayerEquipmentService
{
    private PlayerEquipmentManager _equipment;
    private NetworkedPlayerInventory _inventory;

    private void Awake()
    {
        _equipment = GetComponent<PlayerEquipmentManager>();
        _inventory = GetComponent<NetworkedPlayerInventory>();
    }

    public bool EquipItem(Item item)
    {
        if (item == null || _equipment == null)
            return false;

        if (!NetworkClient.active && !NetworkServer.active)
            return _equipment.EquipItem(item);

        if (!isLocalPlayer)
            return false;

        CmdEquipItem(item.itemName, item.itemType, item.subtype ?? "", item.level);
        return true;
    }

    public bool UnequipItem(Item item)
    {
        if (item == null || _equipment == null)
            return false;

        if (!NetworkClient.active && !NetworkServer.active)
            return _equipment.UnequipItem(item);

        if (!isLocalPlayer)
            return false;

        CmdUnequipItem(item.itemName, item.itemType, item.subtype ?? "", item.level);
        return true;
    }

    public void LogPlayerEquipment()
    {
        _equipment?.LogPlayerEquipment();
    }

    [Command]
    private void CmdEquipItem(string itemName, ItemType itemType, string subtype, int level)
    {
        if (_inventory == null)
            _inventory = GetComponent<NetworkedPlayerInventory>();
        if (_inventory == null)
        {
            Debug.LogWarning("[NetworkedPlayerEquipment] NetworkedPlayerInventory missing on player.");
            return;
        }

        Item serverItem = FindItemInList(_inventory.GetItems(), itemName, itemType, subtype, level);
        if (serverItem == null)
        {
            Debug.LogWarning($"[NetworkedPlayerEquipment] Item not in server inventory: {itemName}");
            return;
        }

        if (!_equipment.EquipItem(serverItem))
            return;

        _inventory.RemoveItem(serverItem);
    }

    [Command]
    private void CmdUnequipItem(string itemName, ItemType itemType, string subtype, int level)
    {
        if (_inventory == null)
            _inventory = GetComponent<NetworkedPlayerInventory>();
        Item eq = _equipment.FindEquippedItemMatching(itemName, itemType, subtype, level);
        if (eq == null)
            return;

        if (!_equipment.UnequipItem(eq))
            return;

        _inventory?.AddItem(eq);
    }

    private static Item FindItemInList(System.Collections.Generic.List<Item> items, string itemName, ItemType itemType, string subtype, int level)
    {
        if (items == null)
            return null;
        string st = subtype ?? "";
        foreach (var it in items)
        {
            if (it == null)
                continue;
            if (it.itemName == itemName && it.itemType == itemType && it.level == level && (it.subtype ?? "") == st)
                return it;
        }
        return null;
    }
}
