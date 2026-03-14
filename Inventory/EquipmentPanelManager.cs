using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class EquipmentPanelManager : MonoBehaviour
{

    [SerializeField] private Transform weaponSection;
    [SerializeField] private Transform generatorSection;
    [SerializeField] private Transform artefactSection;
    
    [SerializeField] private TMP_Text itemDetailsText;
    [SerializeField] private TMP_Text playerStatsReadoutText; // TextMeshPro object for player stats readout

    [SerializeField] private GraphicRaycaster equipmentPanelRaycaster;
    [SerializeField] private EventSystem eventSystem;

    private Dictionary<string, (Image backgroundImage, Image iconImage)> weaponSlots = new Dictionary<string, (Image backgroundImage, Image iconImage)>();
    private Dictionary<string, (Image backgroundImage, Image iconImage)> generatorSlots = new Dictionary<string, (Image backgroundImage, Image iconImage)>();
    private Dictionary<string, (Image backgroundImage, Image iconImage)> artefactSlots = new Dictionary<string, (Image backgroundImage, Image iconImage)>();

    private Dictionary<GameObject, Item> slotToItemMap = new Dictionary<GameObject, Item>();
    public ItemSubtypeMenuManager itemSubtypeMenuManager;

    private IInventoryService _inventoryService;
    private IPlayerStatsService _playerStatsService;
    private IPlayerEquipmentService _playerEquipmentService;
    private IInventoryService InventoryService => _inventoryService ??= GameBootstrap.Locator?.Get<IInventoryService>();
    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private IPlayerEquipmentService PlayerEquipmentService => _playerEquipmentService ??= GameBootstrap.Locator?.Get<IPlayerEquipmentService>();

    private void Start()
    {
        InitializeSlots(weaponSection, weaponSlots);
        InitializeSlots(generatorSection, generatorSlots);
        InitializeSlots(artefactSection, artefactSlots);

        if (equipmentPanelRaycaster == null)
        {
            equipmentPanelRaycaster = GetComponentInParent<GraphicRaycaster>();
            if (equipmentPanelRaycaster == null)
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

        itemSubtypeMenuManager = FindObjectOfType<ItemSubtypeMenuManager>();
        UpdatePlayerStatsReadout();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
        UpdatePlayerStatsReadout();
    }

    private void HandleRightClick()
    {
        if (equipmentPanelRaycaster == null || eventSystem == null)
        {
            Debug.LogError("EquipmentPanelRaycaster or EventSystem is null in HandleRightClick.");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        equipmentPanelRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            RaycastResult clickedResult = results[0];
            GameObject clickedObject = clickedResult.gameObject;

            if (clickedObject.name.EndsWith("IconImage"))
            {
                var item = GetItemFromSlot(clickedObject);
                if (item != null)
                {
                    Debug.Log($"Item to unequip: {item.itemName}");

                    if (InventoryService != null)
                        InventoryService.AddItem(item);
                    PlayerEquipmentService?.UnequipItem(item);

                    // Remove from the Equipment UI
                    UpdateUIOnRightClick(clickedObject, item);

                    // Calculate and update equipment stats
                    PlayerEquipmentService?.CalculateStatsReductionFromEquipment(item);

                    // Update the DisplaySubtypes to reflect the current state
                    if (InventoryService != null)
                        itemSubtypeMenuManager.DisplaySubtypes(InventoryService.GetItemsBySubtype(item.itemType, item.subtype));
                }
            }
        }
    }

    private void HandleLeftClick()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        raycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            RaycastResult clickedResult = results[0];
            GameObject clickedObject = clickedResult.gameObject;

            if (clickedObject.name.EndsWith("IconImage"))
            {
                string slotName = clickedObject.transform.parent.name;
                DisplayItemDetails(slotName);
            }
        }
    }

    private void InitializeSlots(Transform section, Dictionary<string, (Image backgroundImage, Image iconImage)> slots)
    {
        foreach (Transform slot in section)
        {
            if (slot.name.EndsWith("Header")) continue;

            Image backgroundImage = slot.Find(slot.name + "BackgroundImage").GetComponent<Image>();
            Image iconImage = slot.Find(slot.name + "IconImage").GetComponent<Image>();
            slots.Add(slot.name, (backgroundImage, iconImage));
        }
    }

    private void DisplayItemDetails(string slotName)
    {
        Item item = null;
        if (weaponSlots.ContainsKey(slotName))
        {
            item = GetItemFromSlot(weaponSlots[slotName].iconImage.gameObject);
        }
        else if (generatorSlots.ContainsKey(slotName))
        {
            item = GetItemFromSlot(generatorSlots[slotName].iconImage.gameObject);
        }
        else if (artefactSlots.ContainsKey(slotName))
        {
            item = GetItemFromSlot(artefactSlots[slotName].iconImage.gameObject);
        }

        if (item != null)
        {
            string details = GetItemDetails(item);
            itemDetailsText.text = details;
        }
        else
        {
            itemDetailsText.text = "No item selected.";
        }
    }

    private Item GetItemFromSlot(GameObject slot)
    {
        if (slotToItemMap.TryGetValue(slot, out Item item))
        {
            return item;
        }
        return null;
    }


    private string GetItemDetails(Item item)
    {
        return $"Name: {item.itemName}\nType: {item.itemType}\nRarity: {item.itemRarity}\nLevel: {item.level}\nStats: {string.Join(", ", item.stats.Select(stat => $"{stat.statName}: {stat.statValue}"))}";
    }

    public void EquipItem(Item item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon:
                UpdateSlotUI(weaponSlots, item);
                break;
            case ItemType.Phalanx:
                UpdateSlotUI(generatorSlots, item);
                break;
            case ItemType.Artefact:
                UpdateSlotUI(artefactSlots, item);
                break;
        }
    }

    private void UpdateSlotUI(Dictionary<string, (Image backgroundImage, Image iconImage)> slots, Item item)
    {
        foreach (var slot in slots)
        {
            if (slot.Value.iconImage.sprite == null)
            {
                slot.Value.iconImage.sprite = item.icon;
                slot.Value.iconImage.color = Color.white; // Ensure the icon is fully opaque
                slotToItemMap[slot.Value.iconImage.gameObject] = item; // Update the dictionary
                break;
            }
        }
    }

    public void UpdateUIOnRightClick(GameObject slot, Item item)
    {
        itemSubtypeMenuManager = FindObjectOfType<ItemSubtypeMenuManager>();
        if (itemSubtypeMenuManager == null)
        {
            Debug.LogError("ItemSubtypeMenuManager is not found!");
        }

        if (itemSubtypeMenuManager.IsActive())
        {
            Debug.LogError("Populating Subtype Menu. Not an error.");
            // If the SubtypeMenu is active, populate it with the item subtype
            PopulateSubtypeMenu(item);
        }

        var imageComponent = slot.GetComponent<Image>();
        if (imageComponent != null)
        {
            // Reset the icon to null and set the color to transparent
            Debug.LogError("Resetting icon to null and setting color to transparent. not an error.");
            imageComponent.sprite = null;
            imageComponent.color = new Color(0, 0, 0, 0); // Fully transparent
            slotToItemMap.Remove(slot); // Update the dictionary
        }
        else
        {
            Debug.LogError("Slot does not have an Image component!");
        }
    }



    private void PopulateSubtypeMenu(Item item)
    {
        if (InventoryService == null) return;
        List<Item> items = InventoryService.GetItemsBySubtype(item.itemType, item.subtype);
        itemSubtypeMenuManager.DisplaySubtypes(items);
    }

    private void UpdatePlayerStatsReadout()
    {
        if (PlayerStatsService == null) return;
        var playerStats = PlayerStatsService.playerStats;
        if (playerStatsReadoutText != null)
        {
            playerStatsReadoutText.text = FormatPlayerStats(playerStats);
        }
    }

    private string FormatPlayerStats(PlayerStats playerStats)
    {
        List<string> stats = new List<string>
    {
        $"HP: {playerStats.currentHP}/{playerStats.baseHP}",
        $"Shield: {playerStats.currentShield}/{playerStats.baseShield}",
        $"Attack Speed: {playerStats.attackSpeed}",
        $"Damage: {playerStats.currentDamage}",
        $"Attack Range: {playerStats.attackRange}",
        $"Critical Chance: {playerStats.criticalChance}",
        $"Critical Damage: {playerStats.criticalDamage}",
        $"All Damage: {playerStats.allDamage}",
        $"Physical Damage: {playerStats.physicalDamage}",
        $"Ethereal Damage: {playerStats.etherealDamage}",
        $"Demonic Damage: {playerStats.demonicDamage}",
        $"Affliction Damage: {playerStats.afflictionDamage}",
        $"Inevitable Damage: {playerStats.inevitableDamage}",
        $"Max HP: {playerStats.maxHP}",
        $"Max Shield: {playerStats.maxShield}",
        $"Move Speed: {playerStats.moveSpeed}",
        $"EXP Gain: {playerStats.expGain}",
        $"Shield Absorption: {playerStats.shieldAbsorption}",
        $"HP Regeneration: {playerStats.hpRegeneration}",
        $"Shield Regeneration: {playerStats.shieldRegeneration}",
        $"Hit Rate: {playerStats.hitRate}",
        $"Penetration: {playerStats.penetration}",
        $"Integrity: {playerStats.integrity}",
        $"Evasion: {playerStats.evasion}",
        $"Stun Chance: {playerStats.stunChance}",
        $"Status Resistance: {playerStats.statusResistance}",
        $"Damage Reduction: {playerStats.damageReduction}",
        $"Physical Lifesteal: {playerStats.physicalLifesteal}",
        $"Magical Lifesteal: {playerStats.magicalLifesteal}",
        $"Stewardship: {playerStats.stewardship}",
        $"Stealth: {playerStats.stealth}",
        $"Awareness: {playerStats.awareness}",
        $"Ethereal Chance: {playerStats.etherealChance}",
        $"Demonic Chance: {playerStats.demonicChance}",
        $"Affliction Chance: {playerStats.afflictionChance}",
        $"Inevitable Chance: {playerStats.inevitableChance}"
    };

        // Format the stats into 4 columns with adjusted rows to fit new dimensions
        int rows = 8; // Adjusted for more rows due to increased height
        string formattedStats = "";
        for (int i = 0; i < rows; i++)
        {
            for (int j = i; j < stats.Count; j += rows)
            {
                formattedStats += $"{stats[j],-25} "; // Adjusted padding for better alignment
            }
            formattedStats += "\n";
        }

        return formattedStats;
    }

}
