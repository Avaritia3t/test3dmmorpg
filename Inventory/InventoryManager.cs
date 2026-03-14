using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<Resource> playerResources; // List to store player's resources
    public List<Item> playerItems; // List to store player's items
    public List<Item> equippedItems; // List to store player's equipped items
    public List<Rune> playerRunes; // List to store player's runes

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        playerResources = new List<Resource>();
        playerItems = new List<Item>();
        equippedItems = new List<Item>();
        playerRunes = new List<Rune>();

        // Initialize some runes for testing purposes
        playerRunes.Add(new Rune(RuneType.GreenRune, 10, 1.0f));
        playerRunes.Add(new Rune(RuneType.BlueRune, 5, 2.0f));
        playerRunes.Add(new Rune(RuneType.YellowRune, 3, 3.0f));
        playerRunes.Add(new Rune(RuneType.RedRune, 2, 4.0f));
        playerRunes.Add(new Rune(RuneType.PurpleRune, 1, 5.0f));
    }

    public void LogPlayerInventory()
    {
        Debug.Log("Logging Player Inventory:");

        if (playerResources.Count == 0 && playerItems.Count == 0)
        {
            Debug.Log("No resources or items in inventory.");
            return;
        }

        if (playerResources.Count > 0)
        {
            Debug.Log("Resources:");
            foreach (var resource in playerResources)
            {
                Debug.Log($"Type: {resource.type}, Quantity: {resource.quantity}, Grade: {resource.grade}, Regeneration Speed: {resource.regenerationSpeed}");
            }
        }
        else
        {
            Debug.Log("No resources in inventory.");
        }

        if (playerItems.Count > 0)
        {
            Debug.Log("Items:");
            foreach (var item in playerItems)
            {
                Debug.Log($"Name: {item.itemName}, Type: {item.itemType}, Rarity: {item.itemRarity}, Generation Rate: {item.generationRate}");
                foreach (var stat in item.stats)
                {
                    Debug.Log($" - Stat: {stat.statName}, Value: {stat.statValue}");
                }
            }
        }
        else
        {
            Debug.Log("No items in inventory.");
        }
    }

    public List<Item> GetItemsBySubtype(ItemType itemType, string subtype)
    {
        return playerItems.Where(item => item.itemType == itemType && item.subtype == subtype).ToList();
    }

    public void AddResource(Resource resource)
    {
        Resource existingResource = playerResources.Find(r => r.type == resource.type && r.grade == resource.grade);
        if (existingResource != null)
        {
            existingResource.quantity += resource.quantity;
        }
        else
        {
            playerResources.Add(new Resource(resource.type, resource.grade, resource.quantity, resource.regenerationSpeed));
        }
        Debug.Log($"Added resource to inventory: {resource.type}, Quantity: {resource.quantity}");
    }

    public void AddItem(Item item)
    {
        Debug.Log($"Adding item to inventory: {item.itemName}");

        foreach (var stat in item.stats)
        {
            Debug.Log($" - Stat: {stat.statName}, Value: {stat.statValue}");
        }

        playerItems.Add(item);
    }

    public bool RemoveResource(Resource resource, int quantity)
    {
        Resource existingResource = playerResources.Find(r => r.type == resource.type && r.grade == resource.grade);
        if (existingResource != null && existingResource.quantity >= quantity)
        {
            existingResource.quantity -= quantity;
            if (existingResource.quantity == 0)
            {
                playerResources.Remove(existingResource);
            }
            return true;
        }
        return false;
    }

    public void RemoveItem(Item item)
    {
        if (playerItems.Contains(item))
        {
            playerItems.Remove(item);
            Debug.Log($"Removed item: {item.itemName}");
            LogPlayerInventory();
        }
        else
        {
            Debug.LogWarning($"Item not found in inventory: {item.itemName}");
        }
    }

    public void EquipItem(Item item)
    {
        if (playerItems.Remove(item))
        {
            equippedItems.Add(item);
            Debug.Log($"Equipped item: {item.itemName}");
        }
    }

    public void UnequipItem(Item item)
    {
        if (equippedItems.Remove(item))
        {
            playerItems.Add(item);
            Debug.Log($"Unequipped item: {item.itemName}");
        }
    }

    public List<Resource> GetResources()
    {
        return playerResources;
    }

    public List<Item> GetItems()
    {
        return playerItems;
    }

    public int GetResourceQuantity(ResourceType resourceType)
    {
        int quantity = 0;
        foreach (var resource in playerResources)
        {
            if (resource.type == resourceType)
            {
                quantity += resource.quantity;
            }
        }
        return quantity;
    }

    public int GetItemQuantity(ItemType itemType, string subtype = null)
    {
        int count = 0; // Initialize the count variable
        foreach (var item in playerItems)
        {
            // Debug.Log($"Checking item: {item.itemName}, Type: {item.itemType}, Subtype: {item.subtype}");
            if (item.itemType == itemType && (subtype == null || item.subtype == subtype))
            {
                count++; // Increment the count for each matching item
                // Debug.Log($"Matching item found: {item.itemName}, Incremented count: {count}");
            }
        }
        // Debug.Log($"Total count for {itemType} with subtype {subtype}: {count}");
        return count;
    }

    public Item GetItemByName(string itemName)
    {
        foreach (var item in playerItems)
        {
            if (item.itemName == itemName)
            {
                return item;
            }
        }
        return null;
    }

    public int GetRuneQuantity(RuneType runeType)
    {
        int quantity = 0;
        foreach (var rune in playerRunes)
        {
            if (rune.type == runeType)
            {
                quantity += rune.quantity;
            }
        }
        return quantity;
    }

    public float GetRuneMultiplier(RuneType runeType)
    {
        foreach (var rune in playerRunes)
        {
            if (rune.type == runeType)
            {
                return rune.damageMultiplier;
            }
        }
        return 1.0f; // Default multiplier if not found
    }

    public List<Item> GetEquippedItems()
    {
        return equippedItems;
    }

    public Item GetEquippedItem(string itemName)
    {
        return equippedItems.FirstOrDefault(item => item.itemName == itemName);
    }
}
