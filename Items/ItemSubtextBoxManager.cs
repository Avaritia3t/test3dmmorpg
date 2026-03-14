using UnityEngine;
using TMPro;

public class ItemSubtextBoxManager : MonoBehaviour
{
    [SerializeField] private TMP_Text itemSubtextText;
    [SerializeField] private TMP_Text itemSubtextHeaderText;

    private void OnEnable()
    {
        InventoryEvents.OnItemHover += DisplayItemSubtext;
        InventoryEvents.OnItemHoverExit += ClearItemSubtext;
    }

    private void OnDisable()
    {
        InventoryEvents.OnItemHover -= DisplayItemSubtext;
        InventoryEvents.OnItemHoverExit -= ClearItemSubtext;
    }

    // Method to display item subtext
    private void DisplayItemSubtext(Item item)
    {
        if (item == null) return;

        itemSubtextHeaderText.text = item.itemName;
        itemSubtextText.text = $"{item.flavorText}\n";
    }

    // Method to clear item subtext
    private void ClearItemSubtext()
    {
        itemSubtextHeaderText.text = string.Empty;
        itemSubtextText.text = string.Empty;
    }
}
