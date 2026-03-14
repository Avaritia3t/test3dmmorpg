using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType? itemType; // Type of the item in this slot
    public ResourceType? resourceType; // Type of the resource in this slot
    public RuneType? runeType; // Type of the rune in this slot

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("OnPointerEnter triggered.");
        if (resourceType.HasValue)
        {
            int quantity = InventoryManager.Instance.GetResourceQuantity(resourceType.Value);
            string content = $"{resourceType.Value}\nQuantity: {quantity}";
            TooltipManager.Instance.ShowTooltip(content);
        }
        else if (itemType.HasValue)
        {
            int quantity = InventoryManager.Instance.GetItemQuantity(itemType.Value);
            string content = $"{itemType.Value}\nQuantity: {quantity}";
            TooltipManager.Instance.ShowTooltip(content);
        }
        else if (runeType.HasValue)
        {
            int quantity = InventoryManager.Instance.GetRuneQuantity(runeType.Value);
            string content = $"{runeType.Value}\nQuantity: {quantity}\nDamage Multiplier: x{InventoryManager.Instance.GetRuneMultiplier(runeType.Value)}";
            TooltipManager.Instance.ShowTooltip(content);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}
