// Server-authoritative equipment (Mirror Commands). Disabled for Mirror + ParrelSync prototyping:
// register PlayerEquipmentManager on IPlayerEquipmentService (direct equip on host/client).
// To re-enable: change #if false to #if true, prefer registering NetworkedPlayerEquipment in NetworkedLocalPlayerServiceRegistrar.

#if false
using Mirror;
using UnityEngine;

/// <summary>
/// Server-authoritative equipment: local player sends <see cref="Command"/>s; server applies
/// <see cref="PlayerEquipmentManager"/> and mutates <see cref="IInventoryService"/> on the server.
/// Host uses the same InventoryManager instance as the client UI; remote clients need inventory sync (future work).
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(PlayerEquipmentManager))]
public class NetworkedPlayerEquipment : NetworkBehaviour, IPlayerEquipmentService
{
    private PlayerEquipmentManager _equipment;

    private void Awake()
    {
        _equipment = GetComponent<PlayerEquipmentManager>();
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
        var inv = GameBootstrap.Locator?.Get<IInventoryService>();
        if (inv == null)
        {
            Debug.LogWarning("[NetworkedPlayerEquipment] Server has no IInventoryService (locator).");
            return;
        }

        Item serverItem = FindItemInList(inv.GetItems(), itemName, itemType, subtype, level);
        if (serverItem == null)
        {
            Debug.LogWarning($"[NetworkedPlayerEquipment] Item not in server inventory: {itemName}");
            return;
        }

        if (!_equipment.EquipItem(serverItem))
            return;

        inv.RemoveItem(serverItem);
    }

    [Command]
    private void CmdUnequipItem(string itemName, ItemType itemType, string subtype, int level)
    {
        var inv = GameBootstrap.Locator?.Get<IInventoryService>();
        Item eq = _equipment.FindEquippedItemMatching(itemName, itemType, subtype, level);
        if (eq == null)
            return;

        if (!_equipment.UnequipItem(eq))
            return;

        inv?.AddItem(eq);
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
#endif
