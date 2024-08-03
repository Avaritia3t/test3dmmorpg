using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSubtypeMenuManager : MonoBehaviour
{

    private List<Image> subtypeSlotIconImages = new List<Image>();
    private List<Image> subtypeSlotBackgroundImages = new List<Image>();

    [SerializeField] private TMP_Text itemDetailsText;

    [SerializeField] private GraphicRaycaster inventoryPanelRaycaster;
    [SerializeField] private GraphicRaycaster subtypeMenuRaycaster;
    private EventSystem eventSystem;

    private Dictionary<GameObject, Item> slotToItemMap = new Dictionary<GameObject, Item>();

    public EquipmentPanelManager equipmentPanelManager;


    private void Start()
    {
        subtypeMenuRaycaster = GetComponent<GraphicRaycaster>();
        inventoryPanelRaycaster = GameObject.Find("InventoryPanel")?.GetComponent<GraphicRaycaster>();

        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("EventSystem not found in the scene.");
            }
        }

        InitializeSubtypeSlotIconImages();
    }

    private void Update()
    {
        HandleHover();
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
    }

    private void HandleHover()
    {
        if (subtypeMenuRaycaster == null)
        {
            subtypeMenuRaycaster = GetComponent<GraphicRaycaster>();
            if (subtypeMenuRaycaster == null)
            {
                Debug.LogError("SubtypeMenuRaycaster is null in HandleHover.");
                return;
            }
        }

        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("EventSystem is null in HandleHover.");
                return;
            }
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        subtypeMenuRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            List<string> hoveredItems = new List<string>();
            foreach (RaycastResult result in results)
            {
                GameObject hoveredObject = result.gameObject;
                // Debug.Log($"Hovered object: {hoveredObject.name}"); // Debug hovered object

                if (hoveredObject.name.EndsWith("IconImage"))
                {
                    Item item = GetItemFromSlot(hoveredObject);
                    if (item != null)
                    {
                        // Debug.Log($"Item from slot: {item.itemName}"); // Debug item names
                        hoveredItems.Add($"{item.itemName}, Rarity: {item.itemRarity}, Level: {item.level}");
                    }
                    else
                    {
                        Debug.Log("Item is null");
                    }
                }
            }

            if (hoveredItems.Count > 0)
            {
                string content = "[" + string.Join(", ", hoveredItems) + "]";
                TooltipManager.Instance.ShowTooltip(content);
                return;
            }
        }

        TooltipManager.Instance.HideTooltip();
    }

    private Item GetItemFromSlot(GameObject slot)
    {
        if (slotToItemMap.TryGetValue(slot, out Item item))
        {
            string statsList = "[" + string.Join(", ", item.stats.Select(stat => $"{stat.statName}: {stat.statValue}")) + "]";
            // Debug.Log($"Item from slot: [Name: {item.itemName}, Rarity: {item.itemRarity}, Level: {item.level}, Type: {item.itemType}, Stats: {statsList}]");
            return item;
        }
        return null;
    }

    private void HandleRightClick()
    {
        if (subtypeMenuRaycaster == null)
        {
            Debug.LogError("SubtypeMenuRaycaster is null in HandleRightClick.");
            return;
        }

        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("EventSystem is null in HandleRightClick.");
                return;
            }
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        subtypeMenuRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            RaycastResult clickedResult = results[0];
            GameObject clickedObject = clickedResult.gameObject;

            if (clickedObject.name.EndsWith("IconImage"))
            {
                int index = subtypeSlotIconImages.IndexOf(clickedObject.GetComponent<Image>());

                if (index >= 0 && index < InventoryManager.Instance.playerItems.Count)
                {
                    Item item = GetItemFromSlot(clickedObject);

                    if (item != null)
                    {
                        Debug.Log($"Item to equip: {item.itemName}");

                        InventoryManager.Instance.RemoveItem(item);
                        PlayerEquipmentManager.Instance.EquipItem(item);

                        // Call EquipItem in EquipmentPanelManager
                        equipmentPanelManager.EquipItem(item);

                        UpdateUIOnRightClick(clickedObject, item);

                        // Calculate and update equipment stats
                        PlayerEquipmentManager.Instance.CalculateStatsAdditionFromEquipment(item);

                        // Update the DisplaySubtypes to reflect the current state
                        DisplaySubtypes(InventoryManager.Instance.GetItemsBySubtype(item.itemType, item.subtype));
                    }
                    else
                    {
                        Debug.LogError("Item is null. Cannot equip.");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid index: {index}. Cannot equip item.");
                }
            }
        }
    }

    private void HandleLeftClick()
    {
        if (subtypeMenuRaycaster == null || eventSystem == null)
        {
            Debug.LogError("SubtypeMenuRaycaster or EventSystem is null in HandleLeftClick.");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        subtypeMenuRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            RaycastResult clickedResult = results[0];
            GameObject clickedObject = clickedResult.gameObject;

            if (clickedObject.name.EndsWith("IconImage"))
            {
                int index = subtypeSlotIconImages.IndexOf(clickedObject.GetComponent<Image>());

                if (index >= 0 && index < InventoryManager.Instance.playerItems.Count)
                {
                    Item item = GetItemFromSlot(clickedObject);

                    if (item != null)
                    {
                        Debug.Log($"Item details to display: {item.itemName}");
                        DisplayItemDetails(item);
                    }
                }
            }
        }
    }

    private void DisplayItemDetails(Item item)
    {
        if (item != null)
        {
            string details = $"Name: {item.itemName}\n" +
                             $"Type: {item.itemType}\n" +
                             $"Rarity: {item.itemRarity}\n" +
                             $"Level: {item.level}\n" +
                             $"Stats: {string.Join(", ", item.stats.Select(stat => $"{stat.statName}: {stat.statValue}"))}";
            itemDetailsText.text = details;
        }
        else
        {
            itemDetailsText.text = "No item selected.";
        }
    }


    private string GetItemDetails(Item item)
    {
        return $"Name: {item.itemName}\nType: {item.itemType}\nRarity: {item.itemRarity}\nLevel: {item.level}\nStats: {string.Join(", ", item.stats.Select(stat => $"{stat.statName}: {stat.statValue}"))}";
    }


    private void InitializeSubtypeSlotIconImages()
    {
        var allChildren = GetComponentsInChildren<Transform>(true);

        subtypeSlotIconImages = new List<Image>();
        subtypeSlotBackgroundImages = new List<Image>();

        foreach (var child in allChildren)
        {
            if (child.name.EndsWith("IconImage"))
            {
                var iconImage = child.GetComponent<Image>();
                if (iconImage != null)
                {
                    subtypeSlotIconImages.Add(iconImage);
                }
            }
            else if (child.name.EndsWith("BackgroundImage"))
            {
                var backgroundImage = child.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    subtypeSlotBackgroundImages.Add(backgroundImage);
                }
            }
        }

        if (subtypeSlotIconImages == null || subtypeSlotIconImages.Count == 0)
        {
            Debug.LogError("subtypeSlotIconImages is null or empty. Please ensure it is initialized properly.");
        }

        if (subtypeSlotBackgroundImages == null || subtypeSlotBackgroundImages.Count == 0)
        {
            Debug.LogError("subtypeSlotBackgroundImages is null or empty. Please ensure it is initialized properly.");
        }
    }

    public void DisplaySubtypes(List<Item> items)
    {
        Debug.Log("Inside DisplaySubtypes function");

        // Clear the existing mappings
        slotToItemMap.Clear();

        foreach (var backgroundImage in subtypeSlotBackgroundImages)
        {
            backgroundImage.gameObject.SetActive(true);
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("No items found. Hiding all slots.");
            foreach (var slot in subtypeSlotIconImages)
            {
                slot.gameObject.SetActive(false);
            }
            return;
        }

        for (int i = 0; i < subtypeSlotIconImages.Count; i++)
        {
            var slot = subtypeSlotIconImages[i];
            if (i < items.Count)
            {
                slot.sprite = items[i].icon;
                slot.gameObject.SetActive(true);

                // Map the slot to the item
                slotToItemMap[slot.gameObject] = items[i];
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }

        SetRaycasters(false, true);

        // Debug the item subtype
        if (items.Count > 0 && items[0] != null)
        {
            Debug.Log($"Item subtype: {items[0].subtype}");
        }
        else
        {
            Debug.Log("No items of this subtype.");
        }
    }


    private void UpdateUIOnRightClick(GameObject slot, Item item)
    {
        // Set the image component's source image of the slot to the BackgroundEnlarged image
        slot.GetComponent<Image>().sprite = Resources.Load<Sprite>("BackgroundEnlarged");
        // Debug.Log($"Updated slot {slot.name} with BackgroundEnlarged image.");
    }

    public void SetSubtypeMenuActive(bool isActive)
    {
        gameObject.SetActive(isActive);
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isActive);
        }

        if (isActive)
        {
            TooltipManager.Instance.HideTooltip(); // Hide tooltip when the subtype menu is opened
        }
        else
        {
            foreach (var iconImage in subtypeSlotIconImages)
            {
                iconImage.gameObject.SetActive(false);
            }
            foreach (var backgroundImage in subtypeSlotBackgroundImages)
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }

        SetRaycasters(!isActive, isActive);
    }

    public void ToggleItemSubtypeMenu()
    {
        bool isActive = !gameObject.activeSelf;
        SetSubtypeMenuActive(isActive);
    }

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    public List<Image> GetSubtypeSlots()
    {
        return subtypeSlotIconImages;
    }

    private void SetRaycasters(bool inventoryActive, bool subtypeMenuActive)
    {
        if (inventoryPanelRaycaster != null)
        {
            inventoryPanelRaycaster.enabled = inventoryActive;
        }
        if (subtypeMenuRaycaster != null)
        {
            subtypeMenuRaycaster.enabled = subtypeMenuActive;
        }
    }
}
