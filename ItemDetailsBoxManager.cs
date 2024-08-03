using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ItemDetailsBoxManager : MonoBehaviour
{
    [SerializeField] private TMP_Text itemDetailsText;
    [SerializeField] private TMP_Text itemDetailsHeaderText;

    private void OnEnable()
    {
        InventoryEvents.OnItemHover += DisplayItemDetails;
        InventoryEvents.OnItemHoverExit += ClearItemDetails;
    }

    private void OnDisable()
    {
        InventoryEvents.OnItemHover -= DisplayItemDetails;
        InventoryEvents.OnItemHoverExit -= ClearItemDetails;
    }

    // Method to display item details
    private void DisplayItemDetails(Item item)
    {
        if (item == null) return;

        itemDetailsHeaderText.text = item.itemName;
        itemDetailsText.text = $"Rarity: {item.itemRarity}\n" +
                               $"Damage: {item.damageMin} - {item.damageMax}\n" +
                               $"Stats:\n" + GetItemStats(item);
    }

    // Method to clear item details
    private void ClearItemDetails()
    {
        itemDetailsHeaderText.text = string.Empty;
        itemDetailsText.text = string.Empty;
    }

    // Helper method to get item stats as a formatted string
    private string GetItemStats(Item item)
    {
        var stats = item.GetStats();
        string statsString = string.Empty;
        foreach (var stat in stats)
        {
            statsString += $"{stat.Key}: {stat.Value}\n";
        }
        return statsString;
    }
}
