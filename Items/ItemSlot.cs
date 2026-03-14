using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType? itemType; // Type of the item in this slot
    public ResourceType? resourceType; // Type of the resource in this slot
    public RuneType? runeType; // Type of the rune in this slot

    private static IInventoryService _inventoryService;
    private static ITooltipService _tooltipService;
    private static IInventoryService InventoryService => _inventoryService ??= GameBootstrap.Locator?.Get<IInventoryService>();
    private static ITooltipService TooltipService => _tooltipService ??= GameBootstrap.Locator?.Get<ITooltipService>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryService == null) return;

        Debug.Log("OnPointerEnter triggered.");
        if (resourceType.HasValue)
        {
            int quantity = InventoryService.GetResourceQuantity(resourceType.Value);
            string content = $"{resourceType.Value}\nQuantity: {quantity}";
            TooltipService?.ShowTooltip(content);
        }
        else if (itemType.HasValue)
        {
            int quantity = InventoryService.GetItemQuantity(itemType.Value);
            string content = $"{itemType.Value}\nQuantity: {quantity}";
            TooltipService?.ShowTooltip(content);
        }
        else if (runeType.HasValue)
        {
            int quantity = InventoryService.GetRuneQuantity(runeType.Value);
            string content = $"{runeType.Value}\nQuantity: {quantity}\nDamage Multiplier: x{InventoryService.GetRuneMultiplier(runeType.Value)}";
            TooltipService?.ShowTooltip(content);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipService?.HideTooltip();
    }
}
