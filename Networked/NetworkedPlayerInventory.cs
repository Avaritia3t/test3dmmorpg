using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Per-player inventory: server authority, replicated to the owning client via <see cref="SyncList{T}"/>.
/// Add to the player prefab (with <see cref="NetworkIdentity"/>). Registers as <see cref="IInventoryService"/> for the local player only via <see cref="NetworkedLocalPlayerServiceRegistrar"/>.
/// Offline / no Mirror: uses local lists (same behaviour as legacy <see cref="InventoryManager"/>).
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkedPlayerInventory : NetworkBehaviour, IInventoryService
{
    /// <summary>JSON lines of <see cref="ItemPayload"/> — bag only (not equipment UI slots).</summary>
    public readonly SyncList<string> syncItemJson = new SyncList<string>();

    /// <summary>Legacy bag vs "inventory equipped" split (same as <see cref="InventoryManager"/>).</summary>
    public readonly SyncList<string> syncEquippedJson = new SyncList<string>();

    public readonly SyncList<string> syncResourceJson = new SyncList<string>();
    public readonly SyncList<string> syncRuneJson = new SyncList<string>();

    private readonly List<Item> _cachedItems = new List<Item>();
    private readonly List<Item> _cachedEquipped = new List<Item>();
    private readonly List<Resource> _cachedResources = new List<Resource>();
    private readonly List<Rune> _cachedRunes = new List<Rune>();

    private readonly List<Item> _offlineItems = new List<Item>();
    private readonly List<Item> _offlineEquipped = new List<Item>();
    private readonly List<Resource> _offlineResources = new List<Resource>();
    private readonly List<Rune> _offlineRunes = new List<Rune>();

    private PlayerEquipmentManager _equipmentManager;

    private static bool IsOfflinePlay()
    {
        return !NetworkServer.active && !NetworkClient.active;
    }

    private void Awake()
    {
        _equipmentManager = GetComponent<PlayerEquipmentManager>();
        if (IsOfflinePlay())
            InitOfflineDefaults();
    }

    private void InitOfflineDefaults()
    {
        _offlineRunes.Add(new Rune(RuneType.GreenRune, 10, 1.0f));
        _offlineRunes.Add(new Rune(RuneType.BlueRune, 5, 2.0f));
        _offlineRunes.Add(new Rune(RuneType.YellowRune, 3, 3.0f));
        _offlineRunes.Add(new Rune(RuneType.RedRune, 2, 4.0f));
        _offlineRunes.Add(new Rune(RuneType.PurpleRune, 1, 5.0f));
    }

    private void OnSyncInventoryChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        RebuildCachesFromNetwork();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        syncItemJson.Callback += OnSyncInventoryChanged;
        syncEquippedJson.Callback += OnSyncInventoryChanged;
        syncResourceJson.Callback += OnSyncInventoryChanged;
        syncRuneJson.Callback += OnSyncInventoryChanged;

        if (syncRuneJson.Count == 0)
            AddDefaultRunesServer();

        RebuildCachesFromNetwork();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            syncItemJson.Callback += OnSyncInventoryChanged;
            syncEquippedJson.Callback += OnSyncInventoryChanged;
            syncResourceJson.Callback += OnSyncInventoryChanged;
            syncRuneJson.Callback += OnSyncInventoryChanged;
        }
        RebuildCachesFromNetwork();
    }

    private void AddDefaultRunesServer()
    {
        ServerAddRune(new Rune(RuneType.GreenRune, 10, 1.0f));
        ServerAddRune(new Rune(RuneType.BlueRune, 5, 2.0f));
        ServerAddRune(new Rune(RuneType.YellowRune, 3, 3.0f));
        ServerAddRune(new Rune(RuneType.RedRune, 2, 4.0f));
        ServerAddRune(new Rune(RuneType.PurpleRune, 1, 5.0f));
    }

    /// <summary>Called from <see cref="NetworkedPlayerLootReceiver"/> on the server only.</summary>
    public void ServerAddItem(Item item)
    {
        if (!NetworkServer.active || item == null)
            return;
        var payload = InventoryNetConverters.ToPayload(item);
        if (payload == null)
            return;
        syncItemJson.Add(JsonUtility.ToJson(payload));
        RebuildCachesFromNetwork();
    }

    /// <summary>Called from <see cref="NetworkedPlayerLootReceiver"/> on the server only.</summary>
    public void ServerAddResource(Resource resource)
    {
        if (!NetworkServer.active || resource == null)
            return;

        for (int i = 0; i < syncResourceJson.Count; i++)
        {
            var p = JsonUtility.FromJson<ResourcePayload>(syncResourceJson[i]);
            if (p != null && p.type == resource.type && p.grade == resource.grade)
            {
                p.quantity += resource.quantity;
                syncResourceJson[i] = JsonUtility.ToJson(p);
                RebuildCachesFromNetwork();
                return;
            }
        }

        var np = InventoryNetConverters.ToPayload(resource);
        if (np != null)
            syncResourceJson.Add(JsonUtility.ToJson(np));
        RebuildCachesFromNetwork();
    }

    private void ServerAddRune(Rune rune)
    {
        if (rune == null)
            return;
        for (int i = 0; i < syncRuneJson.Count; i++)
        {
            var p = JsonUtility.FromJson<RunePayload>(syncRuneJson[i]);
            if (p != null && p.type == rune.type)
            {
                p.quantity += rune.quantity;
                p.damageMultiplier = rune.damageMultiplier;
                syncRuneJson[i] = JsonUtility.ToJson(p);
                return;
            }
        }
        var np = InventoryNetConverters.ToPayload(rune);
        if (np != null)
            syncRuneJson.Add(JsonUtility.ToJson(np));
    }

    private void RebuildCachesFromNetwork()
    {
        if (IsOfflinePlay())
            return;

        _cachedItems.Clear();
        foreach (var json in syncItemJson)
        {
            var p = JsonUtility.FromJson<ItemPayload>(json);
            var it = InventoryNetConverters.FromPayload(p);
            if (it != null)
            {
                it.icon = ResolveItemIcon(it);
                _cachedItems.Add(it);
            }
        }

        _cachedEquipped.Clear();
        foreach (var json in syncEquippedJson)
        {
            var p = JsonUtility.FromJson<ItemPayload>(json);
            var it = InventoryNetConverters.FromPayload(p);
            if (it != null)
            {
                it.icon = ResolveItemIcon(it);
                _cachedEquipped.Add(it);
            }
        }

        _cachedResources.Clear();
        foreach (var json in syncResourceJson)
        {
            var p = JsonUtility.FromJson<ResourcePayload>(json);
            var r = InventoryNetConverters.FromPayload(p);
            if (r != null)
                _cachedResources.Add(r);
        }

        _cachedRunes.Clear();
        foreach (var json in syncRuneJson)
        {
            var p = JsonUtility.FromJson<RunePayload>(json);
            var ru = InventoryNetConverters.FromPayload(p);
            if (ru != null)
                _cachedRunes.Add(ru);
        }
    }

    private static Sprite ResolveItemIcon(Item item)
    {
        if (item == null)
            return null;
        if (ItemIconManager.Instance != null)
        {
            var s = ItemIconManager.Instance.GetIcon(item.itemName);
            if (s != null)
                return s;
        }
        if (!string.IsNullOrEmpty(item.subtype))
        {
            var path = $"EquipmentIcons/{item.subtype.ToLower()}";
            var sp = Resources.Load<Sprite>(path);
            if (sp != null)
                return sp;
        }
        if (!string.IsNullOrEmpty(item.itemName))
        {
            var path = $"EquipmentIcons/{item.itemName.ToLower()}";
            return Resources.Load<Sprite>(path);
        }
        return null;
    }

    private List<Item> ActiveItems => IsOfflinePlay() ? _offlineItems : _cachedItems;
    private List<Item> ActiveEquippedLegacy => IsOfflinePlay() ? _offlineEquipped : _cachedEquipped;
    private List<Resource> ActiveResources => IsOfflinePlay() ? _offlineResources : _cachedResources;
    private List<Rune> ActiveRunes => IsOfflinePlay() ? _offlineRunes : _cachedRunes;

    public void AddResource(Resource resource)
    {
        if (resource == null)
            return;

        if (IsOfflinePlay())
        {
            OfflineAddResource(resource);
            return;
        }

        if (NetworkServer.active && isServer)
        {
            ServerAddResource(resource);
            return;
        }

        if (isLocalPlayer)
        {
            var rp = InventoryNetConverters.ToPayload(resource);
            if (rp != null)
                CmdAddResource(JsonUtility.ToJson(rp));
        }
    }

    private void OfflineAddResource(Resource resource)
    {
        var existing = _offlineResources.Find(r => r.type == resource.type && r.grade == resource.grade);
        if (existing != null)
            existing.quantity += resource.quantity;
        else
            _offlineResources.Add(new Resource(resource.type, resource.grade, resource.quantity, resource.regenerationSpeed));
    }

    [Command]
    private void CmdAddResource(string resourceJson)
    {
        if (string.IsNullOrEmpty(resourceJson))
            return;
        var payload = JsonUtility.FromJson<ResourcePayload>(resourceJson);
        var r = InventoryNetConverters.FromPayload(payload);
        ServerAddResource(r);
    }

    public void AddItem(Item item)
    {
        if (item == null)
            return;

        if (IsOfflinePlay())
        {
            var copy = CloneItemForBag(item);
            _offlineItems.Add(copy);
            return;
        }

        if (NetworkServer.active && isServer)
        {
            ServerAddItem(item);
            return;
        }

        if (isLocalPlayer)
        {
            var p = InventoryNetConverters.ToPayload(item);
            if (p != null)
                CmdAddItem(JsonUtility.ToJson(p));
        }
    }

    private static Item CloneItemForBag(Item item)
    {
        var payload = InventoryNetConverters.ToPayload(item);
        var clone = InventoryNetConverters.FromPayload(payload);
        if (clone != null)
            clone.icon = ResolveItemIcon(clone);
        return clone;
    }

    [Command]
    private void CmdAddItem(string itemJson)
    {
        if (string.IsNullOrEmpty(itemJson))
            return;
        var payload = JsonUtility.FromJson<ItemPayload>(itemJson);
        var item = InventoryNetConverters.FromPayload(payload);
        ServerAddItem(item);
    }

    public bool RemoveResource(Resource resource, int quantity)
    {
        if (resource == null || quantity <= 0)
            return false;

        if (IsOfflinePlay())
            return OfflineRemoveResource(resource, quantity);

        if (NetworkServer.active && isServer)
            return ServerRemoveResource(resource, quantity);

        if (isLocalPlayer)
        {
            var rp = InventoryNetConverters.ToPayload(resource);
            if (rp != null)
                CmdRemoveResource(JsonUtility.ToJson(rp), quantity);
            return true;
        }

        return false;
    }

    private bool OfflineRemoveResource(Resource resource, int quantity)
    {
        var existing = _offlineResources.Find(r => r.type == resource.type && r.grade == resource.grade);
        if (existing != null && existing.quantity >= quantity)
        {
            existing.quantity -= quantity;
            if (existing.quantity == 0)
                _offlineResources.Remove(existing);
            return true;
        }
        return false;
    }

    private bool ServerRemoveResource(Resource resource, int quantity)
    {
        for (int i = 0; i < syncResourceJson.Count; i++)
        {
            var p = JsonUtility.FromJson<ResourcePayload>(syncResourceJson[i]);
            if (p == null || p.type != resource.type || p.grade != resource.grade)
                continue;
            if (p.quantity < quantity)
                return false;
            p.quantity -= quantity;
            if (p.quantity <= 0)
                syncResourceJson.RemoveAt(i);
            else
                syncResourceJson[i] = JsonUtility.ToJson(p);
            RebuildCachesFromNetwork();
            return true;
        }
        return false;
    }

    [Command]
    private void CmdRemoveResource(string resourceJson, int quantity)
    {
        if (string.IsNullOrEmpty(resourceJson))
            return;
        var payload = JsonUtility.FromJson<ResourcePayload>(resourceJson);
        var r = InventoryNetConverters.FromPayload(payload);
        if (r == null)
            return;
        ServerRemoveResource(r, quantity);
    }

    public void RemoveItem(Item item)
    {
        if (item == null)
            return;

        if (IsOfflinePlay())
        {
            if (_offlineItems.Contains(item))
                _offlineItems.Remove(item);
            return;
        }

        if (NetworkServer.active && isServer)
        {
            ServerRemoveItem(item);
            return;
        }

        if (isLocalPlayer)
        {
            var p = InventoryNetConverters.ToPayload(item);
            if (p != null)
                CmdRemoveItem(JsonUtility.ToJson(p));
        }
    }

    private void ServerRemoveItem(Item item)
    {
        var payload = InventoryNetConverters.ToPayload(item);
        if (payload == null)
            return;

        for (int i = 0; i < syncItemJson.Count; i++)
        {
            var p = JsonUtility.FromJson<ItemPayload>(syncItemJson[i]);
            if (p != null && ItemsMatchPayload(item, p))
            {
                syncItemJson.RemoveAt(i);
                RebuildCachesFromNetwork();
                return;
            }
        }

        for (int i = 0; i < syncEquippedJson.Count; i++)
        {
            var p = JsonUtility.FromJson<ItemPayload>(syncEquippedJson[i]);
            if (p != null && ItemsMatchPayload(item, p))
            {
                syncEquippedJson.RemoveAt(i);
                RebuildCachesFromNetwork();
                return;
            }
        }
    }

    private static bool ItemsMatchPayload(Item item, ItemPayload p)
    {
        return InventoryNetConverters.MatchesPayload(item, p);
    }

    [Command]
    private void CmdRemoveItem(string itemJson)
    {
        if (string.IsNullOrEmpty(itemJson))
            return;
        var payload = JsonUtility.FromJson<ItemPayload>(itemJson);
        var item = InventoryNetConverters.FromPayload(payload);
        ServerRemoveItem(item);
    }

    public void EquipItem(Item item)
    {
        if (item == null)
            return;

        if (IsOfflinePlay())
        {
            if (_offlineItems.Remove(item))
                _offlineEquipped.Add(item);
            return;
        }

        if (NetworkServer.active && isServer)
            ServerEquipItem(item);
        else if (isLocalPlayer)
        {
            var p = InventoryNetConverters.ToPayload(item);
            if (p != null)
                CmdEquipItem(JsonUtility.ToJson(p));
        }
    }

    private void ServerEquipItem(Item item)
    {
        for (int i = 0; i < syncItemJson.Count; i++)
        {
            var p = JsonUtility.FromJson<ItemPayload>(syncItemJson[i]);
            if (p != null && ItemsMatchPayload(item, p))
            {
                var json = syncItemJson[i];
                syncItemJson.RemoveAt(i);
                syncEquippedJson.Add(json);
                RebuildCachesFromNetwork();
                return;
            }
        }
    }

    [Command]
    private void CmdEquipItem(string itemJson)
    {
        if (string.IsNullOrEmpty(itemJson))
            return;
        var payload = JsonUtility.FromJson<ItemPayload>(itemJson);
        var item = InventoryNetConverters.FromPayload(payload);
        ServerEquipItem(item);
    }

    public void UnequipItem(Item item)
    {
        if (item == null)
            return;

        if (IsOfflinePlay())
        {
            if (_offlineEquipped.Remove(item))
                _offlineItems.Add(item);
            return;
        }

        if (NetworkServer.active && isServer)
            ServerUnequipItem(item);
        else if (isLocalPlayer)
        {
            var p = InventoryNetConverters.ToPayload(item);
            if (p != null)
                CmdUnequipItem(JsonUtility.ToJson(p));
        }
    }

    private void ServerUnequipItem(Item item)
    {
        for (int i = 0; i < syncEquippedJson.Count; i++)
        {
            var p = JsonUtility.FromJson<ItemPayload>(syncEquippedJson[i]);
            if (p != null && ItemsMatchPayload(item, p))
            {
                var json = syncEquippedJson[i];
                syncEquippedJson.RemoveAt(i);
                syncItemJson.Add(json);
                RebuildCachesFromNetwork();
                return;
            }
        }
    }

    [Command]
    private void CmdUnequipItem(string itemJson)
    {
        if (string.IsNullOrEmpty(itemJson))
            return;
        var payload = JsonUtility.FromJson<ItemPayload>(itemJson);
        var item = InventoryNetConverters.FromPayload(payload);
        ServerUnequipItem(item);
    }

    public List<Resource> GetResources()
    {
        return ActiveResources;
    }

    public List<Item> GetItems()
    {
        return ActiveItems;
    }

    public List<Item> GetItemsBySubtype(ItemType itemType, string subtype)
    {
        return ActiveItems.Where(i => i != null && i.itemType == itemType && i.subtype == subtype).ToList();
    }

    public int GetResourceQuantity(ResourceType resourceType)
    {
        int quantity = 0;
        foreach (var resource in ActiveResources)
        {
            if (resource != null && resource.type == resourceType)
                quantity += resource.quantity;
        }
        return quantity;
    }

    public int GetItemQuantity(ItemType itemType, string subtype = null)
    {
        int count = 0;
        foreach (var item in ActiveItems)
        {
            if (item != null && item.itemType == itemType && (subtype == null || item.subtype == subtype))
                count++;
        }
        return count;
    }

    public Item GetItemByName(string itemName)
    {
        foreach (var item in ActiveItems)
        {
            if (item != null && item.itemName == itemName)
                return item;
        }
        return null;
    }

    public int GetRuneQuantity(RuneType runeType)
    {
        int quantity = 0;
        foreach (var rune in ActiveRunes)
        {
            if (rune != null && rune.type == runeType)
                quantity += rune.quantity;
        }
        return quantity;
    }

    public float GetRuneMultiplier(RuneType runeType)
    {
        foreach (var rune in ActiveRunes)
        {
            if (rune != null && rune.type == runeType)
                return rune.damageMultiplier;
        }
        return 1.0f;
    }

    /// <summary>Equipped gear from <see cref="PlayerEquipmentManager"/> when present; otherwise legacy equipped list.</summary>
    public List<Item> GetEquippedItems()
    {
        if (_equipmentManager != null && _equipmentManager.equippedItems != null && _equipmentManager.equippedItems.Count > 0)
            return new List<Item>(_equipmentManager.equippedItems);

        return new List<Item>(ActiveEquippedLegacy);
    }

    public Item GetEquippedItem(string itemName)
    {
        foreach (var item in GetEquippedItems())
        {
            if (item != null && item.itemName == itemName)
                return item;
        }
        return null;
    }
}
