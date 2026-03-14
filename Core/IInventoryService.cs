using System.Collections.Generic;

public interface IInventoryService
{
    void AddResource(Resource resource);
    void AddItem(Item item);
    bool RemoveResource(Resource resource, int quantity);
    void RemoveItem(Item item);
    void EquipItem(Item item);
    void UnequipItem(Item item);

    List<Resource> GetResources();
    List<Item> GetItems();
    List<Item> GetItemsBySubtype(ItemType itemType, string subtype);
    int GetResourceQuantity(ResourceType resourceType);
    int GetItemQuantity(ItemType itemType, string subtype = null);
    Item GetItemByName(string itemName);
    int GetRuneQuantity(RuneType runeType);
    float GetRuneMultiplier(RuneType runeType);
    List<Item> GetEquippedItems();
    Item GetEquippedItem(string itemName);
}
