using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private bool isInventoryOpen = false;
    private bool isSubtypeMenuActive = false;
    private List<GameObject> inventoryObjects = new List<GameObject>();
    private List<TMP_Text> textElements = new List<TMP_Text>();
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;

    // Add new references
    private GraphicRaycaster inventoryRaycaster;
    private GraphicRaycaster itemSubtypeRaycaster;

    // Arrays for storing references to images
    private Image[] subtypeWeaponSlotIconImages;
    private Image[] subtypeWeaponSlotBackgroundImages;
    private Image[] weaponSlotIconImages;
    private Image[] weaponSlotBackgroundImages;
    private Image[] generatorSlotIconImages;
    private Image[] artefactSlotIconImages;

    public GameObject inventoryPanel; // Reference to InventoryPanel
    public GameObject equipmentPanel;
    public GameObject itemSubtypeMenu; // Reference to ItemSubtypeMenu

    // Fields for Item Details and Subtext components
    public TMP_Text itemDetailsText;
    public TMP_Text itemSubtextText;

    private IInventoryService _inventoryService;
    private IPlayerStatsService _playerStatsService;
    private ITooltipService _tooltipService;
    private IInventoryService InventoryService => _inventoryService ??= GameBootstrap.Locator?.Get<IInventoryService>();
    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private ITooltipService TooltipService => _tooltipService ??= GameBootstrap.Locator?.Get<ITooltipService>();

    private void Start()
    {
        raycaster = GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        if (raycaster == null)
        {
            Debug.LogError("GraphicRaycaster is not found on the InventoryCanvas.");
        }

        if (eventSystem == null)
        {
            Debug.LogError("EventSystem is not found in the scene.");
        }

        // Find all child objects that need to be toggled
        inventoryObjects.AddRange(GetComponentsInChildren<Transform>(true)
                                  .Where(t => t.gameObject != this.gameObject) // Exclude the InventoryCanvas itself
                                  .Select(t => t.gameObject));
        textElements.AddRange(GetComponentsInChildren<TMP_Text>(true));

        // Assign Item Details and Subtext components
        itemDetailsText = GameObject.Find("ItemDetailsText").GetComponent<TMP_Text>();
        itemSubtextText = GameObject.Find("ItemSubtextText").GetComponent<TMP_Text>();

        // Initialize raycasters
        inventoryRaycaster = inventoryPanel.GetComponent<GraphicRaycaster>();
        itemSubtypeRaycaster = itemSubtypeMenu.GetComponent<GraphicRaycaster>();

        if (inventoryRaycaster == null)
        {
            Debug.LogError("GraphicRaycaster is not found on the InventoryPanel.");
        }

        if (itemSubtypeRaycaster == null)
        {
            Debug.LogError("GraphicRaycaster is not found on the ItemSubtypeMenu.");
        }

        // Hide ItemSubtypeMenu initially
        hideSubtypeMenu();

        // Initialize weaponSlotIconImages
        InitializeSubtypeSlotIconImages();

        // Ensure subtypeWeaponSlotBackgroundImages is not null and has elements
        if (subtypeWeaponSlotBackgroundImages == null)
        {
            Debug.LogError("subtypeWeaponSlotBackgroundImages is null. Please ensure it is initialized properly.");
        }
        else if (subtypeWeaponSlotBackgroundImages.Length == 0)
        {
            Debug.LogError("subtypeWeaponSlotBackgroundImages is initialized but contains no elements.");
        }
        else
        {
            Debug.Log($"subtypeWeaponSlotBackgroundImages initialized with {subtypeWeaponSlotBackgroundImages.Length} elements.");
        }

        InitializeEquipmentSlotIcons();

        // Hide the inventory canvas initially
        SetInventoryObjectsActive(false);

        Debug.Log("Start function completed: Inventory objects and text elements initialized.");
    }


    private void EnableRaycaster(GraphicRaycaster raycaster)
    {
        if (raycaster != null)
        {
            raycaster.enabled = true;
        }
    }

    private void DisableRaycaster(GraphicRaycaster raycaster)
    {
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (isInventoryOpen)
        {
            HandleHover();
            if (Input.GetKeyDown(KeyCode.S))
            {
                ToggleSubtypeMenu();
            }
        }
    }

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        SetInventoryObjectsActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            EnableRaycaster(inventoryRaycaster);
            DisableRaycaster(itemSubtypeRaycaster);

            isSubtypeMenuActive = false;

            Debug.Log("Inventory opened: Raycaster states updated.");
        }
        else
        {
            if (TooltipService == null)
            {
                Debug.LogError("ITooltipService is null when trying to hide tooltip.");
            }
            else
            {
                TooltipService.HideTooltip();
            }
            hideSubtypeMenu();
            Debug.Log("Inventory closed: Tooltip hidden and subtype menu hidden.");
        }
    }

    private void SetInventoryObjectsActive(bool isActive)
    {
        foreach (var obj in inventoryObjects)
        {
            if (obj != itemSubtypeMenu)
            {
                obj.SetActive(isActive);
            }
        }

        if (!isActive)
        {
            itemSubtypeMenu.SetActive(false);
        }

        Debug.Log($"Inventory objects set to active: {isActive}");
    }


    private void HandleHover()
    {
        if (raycaster == null || eventSystem == null)
        {
            Debug.LogError("Raycaster or EventSystem is null in HandleHover.");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        LayerMask activeLayerMask = isSubtypeMenuActive ? LayerMask.GetMask("SubtypeMenuUI") : LayerMask.GetMask("UI");
        var filteredResults = results.Where(result => activeLayerMask == (activeLayerMask | (1 << result.gameObject.layer))).ToList();

        if (filteredResults.Count == 0)
        {
            ClearItemDetails();
            if (TooltipService == null)
            {
                Debug.LogError("ITooltipService is null when trying to hide tooltip in HandleHover.");
            }
            else
            {
                TooltipService.HideTooltip();
            }
            return;
        }

        foreach (RaycastResult result in filteredResults)
        {
            GameObject hoveredObject = result.gameObject;
            // Debug.Log("Hovered over: " + hoveredObject.name);

            if (hoveredObject.name.EndsWith("SlotImage"))
            {
                string itemName = hoveredObject.name.Replace("SlotImage", "");
                string content = GetTooltipContent(itemName);
                TooltipService.ShowTooltip(content);

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
                        showSubtypeMenu();
                    }
                    else if (System.Enum.TryParse(itemName, out phalanxType))
                    {
                        Debug.Log("Attempting to manipulate UI for PhalanxType");
                        PopulateItemSubtypeMenu(ItemType.Phalanx, itemName);
                        showSubtypeMenu();
                    }
                    else if (System.Enum.TryParse(itemName, out artefactType))
                    {
                        Debug.Log("Attempting to manipulate UI for ArtefactType");
                        PopulateItemSubtypeMenu(ItemType.Artefact, itemName);
                        showSubtypeMenu();
                    }
                    else
                    {
                        Debug.LogWarning("Failed to parse itemName: " + itemName);
                    }
                }
                return;
            }
        }

        if (TooltipService == null)
        {
            Debug.LogError("ITooltipService is null when trying to hide tooltip in HandleHover (final).");
        }
        else
        {
            TooltipService.HideTooltip();
        }
    }

    private string GetTooltipContent(string itemName)
    {
        ResourceType resourceType;
        WeaponType weaponType;
        PhalanxType phalanxType;
        ArtefactType artefactType;
        RuneType runeType;
        string content = "";

        if (InventoryService == null) return content;

        if (System.Enum.TryParse(itemName, out resourceType))
        {
            int quantity = InventoryService.GetResourceQuantity(resourceType);
            content = $"{resourceType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out weaponType))
        {
            int quantity = InventoryService.GetItemQuantity(ItemType.Weapon, itemName);
            content = $"{weaponType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out phalanxType))
        {
            int quantity = InventoryService.GetItemQuantity(ItemType.Phalanx, itemName);
            content = $"{phalanxType}\nQuantity: {quantity}";
        }
        else if (System.Enum.TryParse(itemName, out artefactType))
        {
            int quantity = InventoryService.GetItemQuantity(ItemType.Artefact, itemName);
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

    private void InitializeSubtypeSlotIconImages()
    {
        Debug.Log("Inside InitializeSubtypeSlotIconImages");
        subtypeWeaponSlotIconImages = GetComponentsInChildren<Transform>(true)
                                      .Where(t => t.name.StartsWith("SubtypeSlot"))
                                      .Select(t => t.GetComponentsInChildren<Image>(true)
                                                    .FirstOrDefault(i => i.name.EndsWith("IconImage")))
                                      .Where(i => i != null)
                                      .ToArray();

        subtypeWeaponSlotBackgroundImages = GetComponentsInChildren<Transform>(true)
                                            .Where(t => t.name.StartsWith("SubtypeSlot"))
                                            .Select(t => t.GetComponentsInChildren<Image>(true)
                                                          .FirstOrDefault(i => i.name.EndsWith("BackgroundImage")))
                                            .Where(i => i != null)
                                            .ToArray();

        if (subtypeWeaponSlotIconImages == null || subtypeWeaponSlotIconImages.Length == 0)
        {
            Debug.LogError("subtypeWeaponSlotIconImages is null or empty. Please ensure it is initialized properly.");
        }

        if (subtypeWeaponSlotBackgroundImages == null || subtypeWeaponSlotBackgroundImages.Length == 0)
        {
            Debug.LogError("subtypeWeaponSlotBackgroundImages is null or empty. Please ensure it is initialized properly.");
        }

        foreach (var img in subtypeWeaponSlotIconImages)
        {
            Debug.Log("Initialized subtype slot icon: " + img.name);
        }
    }

    private void InitializeEquipmentSlotIcons()
    {
        GameObject equipmentPanel = GameObject.Find("EquipmentPanel");
        if (equipmentPanel == null)
        {
            Debug.LogError("EquipmentPanel not found in the scene.");
            return;
        }

        weaponSlotIconImages = equipmentPanel.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("WeaponSlot"))
            .SelectMany(t => t.GetComponentsInChildren<Image>(true))
            .Where(i => i.name.EndsWith("IconImage"))
            .Distinct()
            .ToArray();
        Debug.Log("Initialized " + weaponSlotIconImages.Length + " weapon slots:");
        foreach (var img in weaponSlotIconImages)
        {
            Debug.Log(img.name);
            AddEventListeners(img.gameObject, null, true);
        }

        generatorSlotIconImages = equipmentPanel.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("GenSlot"))
            .SelectMany(t => t.GetComponentsInChildren<Image>(true))
            .Where(i => i.name.EndsWith("IconImage"))
            .Distinct()
            .ToArray();
        Debug.Log("Initialized " + generatorSlotIconImages.Length + " generator slots:");
        foreach (var img in generatorSlotIconImages)
        {
            Debug.Log(img.name);
            AddEventListeners(img.gameObject, null, true);
        }

        artefactSlotIconImages = equipmentPanel.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("ArtefactSlot"))
            .SelectMany(t => t.GetComponentsInChildren<Image>(true))
            .Where(i => i.name.EndsWith("IconImage"))
            .Distinct()
            .ToArray();
        Debug.Log("Initialized " + artefactSlotIconImages.Length + " artefact slots:");
        foreach (var img in artefactSlotIconImages)
        {
            Debug.Log(img.name);
            AddEventListeners(img.gameObject, null, true);
        }
    }

    private void PopulateItemSubtypeMenu(ItemType itemType, string subtype)
    {
        if (InventoryService == null) return;

        Debug.Log("Inside PopulateItemSubtypeMenu function");
        List<Item> items = InventoryService.GetItemsBySubtype(itemType, subtype);

        Debug.Log($"Found {items.Count} items of subtype {subtype}");
        foreach (var item in items)
        {
            Debug.Log($"Item: {item.itemName}, Icon: {item.icon}");
        }

        foreach (var backgroundImage in subtypeWeaponSlotBackgroundImages)
        {
            backgroundImage.gameObject.SetActive(true);
        }

        if (items.Count == 0)
        {
            Debug.LogWarning($"No items found for subtype {subtype}. Hiding all slots.");
            foreach (var slot in subtypeWeaponSlotIconImages)
            {
                slot.gameObject.SetActive(false);
            }
            return;
        }

        for (int i = 0; i < subtypeWeaponSlotIconImages.Length; i++)
        {
            var slot = subtypeWeaponSlotIconImages[i];
            if (i < items.Count)
            {
                slot.sprite = items[i].icon;
                slot.gameObject.SetActive(true);

                AddEventListeners(slot.gameObject, items[i]);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void AddEventListeners(GameObject slot, Item item, bool isEquippedSlot = false)
    {
        EventTrigger trigger = slot.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = slot.AddComponent<EventTrigger>();
        }
        else
        {
            trigger.triggers.Clear();
        }

        if (item != null)
        {
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            pointerEnter.callback.AddListener((eventData) => ShowItemDetails(item));
            trigger.triggers.Add(pointerEnter);

            EventTrigger.Entry rightClick = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            rightClick.callback.AddListener((eventData) =>
            {
                PointerEventData pointerEventData = (PointerEventData)eventData;
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    Debug.Log("Right-click detected on slot: " + slot.name);
                    if (isEquippedSlot)
                    {
                        Debug.Log("Unequipping item: " + item.itemName);
                        UnequipItem(item, slot);
                    }
                    else
                    {
                        Debug.Log("Equipping item: " + item.itemName);
                        EquipItem(item);
                    }
                }
            });
            trigger.triggers.Add(rightClick);
        }
        else
        {
            Debug.LogWarning("Attempted to add event listeners to a slot with a null item.");
        }
    }

    private void ShowItemDetails(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("No item to show details for.");
            return;
        }

        string details = $"{item.itemName}\nRarity: {item.itemRarity}\nDamage: {item.damageMin} - {item.damageMax}\n\nStats:";
        // Add logic to display item details in UI
    }


    private void ClearItemDetails()
    {
        itemDetailsText.text = string.Empty;
        itemSubtextText.text = string.Empty;
    }

    private void EquipItem(Item item)
    {
        Debug.Log("Equipping item: " + item.itemName);

        // Find the first empty slot in the equipment panel
        Image[] equipmentSlots = GetEquipmentSlots(item);
        if (equipmentSlots == null)
        {
            Debug.LogError("No equipment slots found for item type: " + item.itemType);
            return;
        }

        Image emptySlot = equipmentSlots.FirstOrDefault(slot => slot.sprite == null);
        if (emptySlot == null)
        {
            Debug.LogError("No empty slots available in the equipment panel.");
            return;
        }

        // Find the icon image in the Item Subtype Menu that matches the equipped item
        Image[] subtypeSlots = GetSubtypeSlots(item);
        if (subtypeSlots == null)
        {
            Debug.LogError("No subtype slots found for item type: " + item.itemType);
            return;
        }

        Image subtypeSlot = subtypeSlots.FirstOrDefault(slot => slot.sprite == item.icon);
        if (subtypeSlot == null)
        {
            Debug.LogError("No matching item found in the subtype inventory.");
            return;
        }

        // Equip the item: move the icon to the equipment slot and clear the subtype slot
        emptySlot.sprite = item.icon;
        emptySlot.color = Color.white;

        subtypeSlot.sprite = null;
        subtypeSlot.color = new Color(1, 1, 1, 0); // Set to transparent

        // Optionally, update item data in your inventory/equipment system

        if (InventoryService != null)
            InventoryService.RemoveItem(item);
        if (PlayerStatsService != null)
            PlayerStatsService.EquipItem(item);
    }

    private void UnequipItem(Item item, GameObject equippedSlot)
    {
        Debug.Log("Unequipping item: " + item.itemName);

        // Find the first empty slot in the subtype inventory
        Image[] subtypeSlots = GetSubtypeSlots(item);
        if (subtypeSlots == null)
        {
            Debug.LogError("No subtype slots found for item type: " + item.itemType);
            return;
        }

        Image emptySubtypeSlot = subtypeSlots.FirstOrDefault(slot => slot.sprite == null);
        if (emptySubtypeSlot == null)
        {
            Debug.LogError("No empty slots available in the subtype inventory.");
            return;
        }

        // Find the icon image in the Equipment Panel that matches the unequipped item
        Image[] equipmentSlots = GetEquipmentSlots(item);
        if (equipmentSlots == null)
        {
            Debug.LogError("No equipment slots found for item type: " + item.itemType);
            return;
        }

        Image equippedIcon = equipmentSlots.FirstOrDefault(slot => slot.sprite == item.icon);
        if (equippedIcon == null)
        {
            Debug.LogError("No matching item found in the equipment panel.");
            return;
        }

        // Unequip the item: move the icon back to the subtype slot and clear the equipment slot
        emptySubtypeSlot.sprite = item.icon;
        emptySubtypeSlot.color = Color.white;

        equippedIcon.sprite = null;
        equippedIcon.color = new Color(1, 1, 1, 0); // Set to transparent

        if (InventoryService != null)
            InventoryService.AddItem(item);
        if (PlayerStatsService != null)
            PlayerStatsService.UnequipItem(item);

        // Set the item as equippable again
        item.isEquippable = true;

        // Optionally, update item data in your inventory/equipment system
    }

    private Image[] GetEquipmentSlots(Item item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon:
                return weaponSlotIconImages;
            case ItemType.Phalanx:
                return generatorSlotIconImages;
            case ItemType.Artefact:
                return artefactSlotIconImages;
            default:
                Debug.LogError("Unknown item type: " + item.itemType);
                return null;
        }
    }

    private Image[] GetSubtypeSlots(Item item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon:
                return subtypeWeaponSlotIconImages;
            case ItemType.Phalanx:
                return generatorSlotIconImages; // Assuming similar structure for generators
            case ItemType.Artefact:
                return artefactSlotIconImages; // Assuming similar structure for artefacts
            default:
                Debug.LogError("Unknown item type: " + item.itemType);
                return null;
        }
    }

    public void UpdateDetailsDisplay(string details)
    {
        foreach (var textElement in textElements)
        {
            if (textElement.name == "DetailsText")
            {
                textElement.text = details;
                break;
            }
        }
    }

    public void UpdateEquipmentDisplay()
    {
        Debug.Log("Inside UpdateEquipmentDisplay.");

        foreach (var textElement in textElements)
        {
            if (textElement.name == "CurrentEquipmentText")
            {
                textElement.text = "Current Equipment:\n";

                if (PlayerStatsService == null) break;
                foreach (var slot in PlayerStatsService.playerStats.equipmentSlots)
                {
                    foreach (var item in slot.equippedItems)
                    {
                        textElement.text += $"Slot: {slot.slotType}, Item: {item.itemName}\n";
                    }
                }

                break;
            }
        }
    }

    public void UpdatePlayerStatsDisplay()
    {
        Debug.Log("Inside UpdatePlayerStatsDisplay");

        foreach (var textElement in textElements)
        {
            if (textElement.name == "PlayerStatsText")
            {
                textElement.text = "Player Stats:\n";

                if (PlayerStatsService == null) break;
                PlayerStats stats = PlayerStatsService.playerStats;

                textElement.text += $"HP: {stats.baseHP}, Shield: {stats.baseShield}, Attack Speed: {stats.attackSpeed}, Damage: {stats.currentDamage}\n";

                break;
            }
        }
    }

    // Functions to hide and show the ItemSubtypeMenu
    private void hideSubtypeMenu()
    {
        if (itemSubtypeMenu != null)
        {
            itemSubtypeMenu.SetActive(false);
            SetAllChildrenActive(itemSubtypeMenu, false);

            // Disable subtype menu raycaster
            DisableRaycaster(itemSubtypeRaycaster);
            // Enable inventory panel raycaster if the inventory is open
            if (isInventoryOpen)
            {
                EnableRaycaster(inventoryRaycaster);
            }

            isSubtypeMenuActive = false; // Set subtype menu as not active
        }
        else
        {
            Debug.LogError("ItemSubtypeMenu not found in the scene.");
        }
    }

    private void showSubtypeMenu()
    {
        if (itemSubtypeMenu != null)
        {
            itemSubtypeMenu.SetActive(true);
            SetAllChildrenActive(itemSubtypeMenu, true);

            // Enable subtype menu raycaster
            EnableRaycaster(itemSubtypeRaycaster);
            // Disable inventory panel raycaster
            DisableRaycaster(inventoryRaycaster);

            isSubtypeMenuActive = true; // Set subtype menu as active
        }
        else
        {
            Debug.LogError("ItemSubtypeMenu not found in the scene.");
        }
    }

    private void SetAllChildrenActive(GameObject parent, bool isActive)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }

    private void ToggleSubtypeMenu()
    {
        if (itemSubtypeMenu != null)
        {
            bool isActive = itemSubtypeMenu.activeSelf;
            if (isActive)
            {
                hideSubtypeMenu();
            }
            else
            {
                showSubtypeMenu();
            }
        }
        else
        {
            Debug.LogError("ItemSubtypeMenu not found in the scene.");
        }
    }
}

