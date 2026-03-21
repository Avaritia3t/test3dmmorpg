using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class InventoryPanelManager : MonoBehaviour
{
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Transform itemSlotParent;

    private bool isSubtypeMenuOpen = false;
    private ItemSubtypeMenuManager itemSubtypeMenuManager;

    private IInventoryService _inventoryService;
    private ITooltipService _tooltipService;

    private void Awake()
    {
        var locator = GameBootstrap.Locator;
        if (locator != null)
        {
            _inventoryService = locator.Get<IInventoryService>();
            _tooltipService = locator.Get<ITooltipService>();
        }

        itemSubtypeMenuManager = GetComponentInChildren<ItemSubtypeMenuManager>(true);
        if (itemSubtypeMenuManager == null)
            itemSubtypeMenuManager = transform.root.GetComponentInChildren<ItemSubtypeMenuManager>(true);
    }

    private void Start()
    {
        // Dynamically assign the raycaster and eventSystem if they are not already assigned
        if (raycaster == null)
        {
            raycaster = GetComponentInParent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError("GraphicRaycaster not found in parent hierarchy.");
            }
        }

        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("EventSystem not found in the scene.");
            }
        }

        // Dynamically find and assign the ItemSubtypeMenuManager instance
        itemSubtypeMenuManager = FindObjectOfType<ItemSubtypeMenuManager>();
        if (itemSubtypeMenuManager == null)
        {
            Debug.LogError("ItemSubtypeMenuManager instance not found.");
        }
    }

    private void Update()
    {
        if (itemSubtypeMenuManager == null || !itemSubtypeMenuManager.gameObject.activeSelf)
        {
            HandleHover();
        }
    }

    private void HandleHover()
    {
        // Check which specific component is null
        if (raycaster == null)
        {
            Debug.LogError("Raycaster is null in HandleHover.");
            return;
        }

        // Check and assign the EventSystem if it's null
        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("EventSystem is still null after attempting to assign it in HandleHover.");
                return;
            }
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        LayerMask activeLayerMask = isSubtypeMenuOpen ? LayerMask.GetMask("SubtypeMenuUI") : LayerMask.GetMask("UI");
        var filteredResults = results.Where(result => activeLayerMask == (activeLayerMask | (1 << result.gameObject.layer))).ToList();

        if (filteredResults.Count == 0)
        {
            _tooltipService?.HideTooltip();
            return;
        }

        foreach (RaycastResult result in filteredResults)
        {
            GameObject hoveredObject = result.gameObject;
            if (hoveredObject.name.EndsWith("SlotImage"))
            {
                string itemName = hoveredObject.name.Replace("SlotImage", "");
                string content = GetTooltipContent(itemName);
                _tooltipService?.ShowTooltip(content);

                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("MouseButtonClicked on: " + itemName);
                    WeaponType weaponType;
                    PhalanxType phalanxType;
                    ArtefactType artefactType;

                    if (System.Enum.TryParse(itemName, out weaponType))
                    {
                        Debug.Log("Attempting to manipulate UI for WeaponType");
                        PopulateItemSubtypeMenu(ItemType.Weapon, itemName);
                        itemSubtypeMenuManager.ToggleItemSubtypeMenu();
                    }
                    else if (System.Enum.TryParse(itemName, out phalanxType))
                    {
                        Debug.Log("Attempting to manipulate UI for PhalanxType");
                        PopulateItemSubtypeMenu(ItemType.Phalanx, itemName);
                        itemSubtypeMenuManager.ToggleItemSubtypeMenu();
                    }
                    else if (System.Enum.TryParse(itemName, out artefactType))
                    {
                        Debug.Log("Attempting to manipulate UI for ArtefactType");
                        PopulateItemSubtypeMenu(ItemType.Artefact, itemName);
                        itemSubtypeMenuManager.ToggleItemSubtypeMenu();
                    }
                    else
                    {
                        Debug.LogWarning("Failed to parse itemName: " + itemName);
                    }
                }
                return;
            }
        }

        _tooltipService?.HideTooltip();
    }

    private string GetTooltipContent(string itemName)
    {
        ResourceType resourceType;
        WeaponType weaponType;
        PhalanxType phalanxType;
        ArtefactType artefactType;
        RuneType runeType;
        string content = "";

        if (_inventoryService == null) return content;

        if (System.Enum.TryParse(itemName, out resourceType))
        {
            int quantity = _inventoryService.GetResourceQuantity(resourceType);
            content = $"{resourceType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out weaponType))
        {
            int quantity = _inventoryService.GetItemQuantity(ItemType.Weapon, itemName);
            content = $"{weaponType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out phalanxType))
        {
            int quantity = InventoryService.GetItemQuantity(ItemType.Phalanx, itemName);
            content = $"{phalanxType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out artefactType))
        {
            int quantity = _inventoryService.GetItemQuantity(ItemType.Artefact, itemName);
            content = $"{artefactType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out runeType))
        {
            int quantity = InventoryService.GetRuneQuantity(runeType);
            float multiplier = InventoryService.GetRuneMultiplier(runeType);
            content = $"{runeType}\nQuantity: {quantity}\nDamage Multiplier: x{multiplier}";
        }
        else
        {
            Debug.LogWarning($"ItemName '{itemName}' could not be parsed as Resource, Item, or Rune");
        }

        return content;
    }

    private void PopulateItemSubtypeMenu(ItemType itemType, string subtype)
    {
        if (_inventoryService == null) return;

        Debug.Log("Inside PopulateItemSubtypeMenu function");
        List<Item> items = _inventoryService.GetItemsBySubtype(itemType, subtype);

        Debug.Log($"Found {items.Count} items of subtype {subtype}");
        foreach (var item in items)
        {
            Debug.Log($"Item: {item.itemName}, Icon: {item.icon}");
        }

        itemSubtypeMenuManager.DisplaySubtypes(items);
    }
}
