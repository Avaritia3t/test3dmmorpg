using UnityEngine;
using UnityEngine.UI;

public class InventoryCanvasManager : MonoBehaviour
{
    public static InventoryCanvasManager Instance { get; private set; } // Singleton instance

    [SerializeField] private GameObject inventoryCanvas; // Reference to the entire inventory canvas
    [SerializeField] private GameObject itemSubtypeMenu;
    private GraphicRaycaster inventoryRaycaster;
    private GraphicRaycaster itemSubtypeRaycaster;

    private bool isInventoryOpen = false;
    // private bool isSubtypeMenuOpen = false;

    public ItemSubtypeMenuManager itemSubtypeMenuManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Debug.Log("InventoryCanvasManager Start method called.");

        // Dynamically add GraphicRaycaster if not present
        inventoryRaycaster = inventoryCanvas.GetComponent<GraphicRaycaster>();
        if (inventoryRaycaster == null)
        {
            inventoryRaycaster = inventoryCanvas.AddComponent<GraphicRaycaster>();
        }

        itemSubtypeRaycaster = itemSubtypeMenu.GetComponent<GraphicRaycaster>();
        if (itemSubtypeRaycaster == null)
        {
            itemSubtypeRaycaster = itemSubtypeMenu.AddComponent<GraphicRaycaster>();
        }

        // Ensure inventory starts inactive
        SetCanvasActive(false);
        DisableRaycaster(inventoryRaycaster);
        DisableRaycaster(itemSubtypeRaycaster);

        // Ensure itemSubtypeMenu starts inactive
        itemSubtypeMenu.SetActive(false);

        // Debug.Log("InventoryCanvasManager initialization complete.");
    }

    private void Update()
    {
        // Toggle inventory visibility with the I key
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("I key pressed.");
            ToggleInventory();
        }

        // Toggle item subtype menu with the S key if inventory is open
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("S key pressed.");
            itemSubtypeMenuManager.ToggleItemSubtypeMenu();
        }
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

    // Method to toggle inventory
    private void ToggleInventory()
    {
        Debug.Log("ToggleInventory method called.");
        isInventoryOpen = !isInventoryOpen;
        SetCanvasActive(isInventoryOpen);
        if (isInventoryOpen)
        {
            EnableRaycaster(inventoryRaycaster);
            DisableRaycaster(itemSubtypeRaycaster);
        }
        else
        {
            DisableRaycaster(inventoryRaycaster);
            DisableRaycaster(itemSubtypeRaycaster);
            // Ensure itemSubtypeMenu is hidden when closing inventory
            itemSubtypeMenuManager.SetSubtypeMenuActive(false);
            // Hide the tooltip when closing the inventory
            TooltipManager.Instance.HideTooltip();
        }
        Debug.Log("Inventory state toggled. isInventoryOpen: " + isInventoryOpen);
    }


    // Method to set the entire canvas and its children active/inactive
    private void SetCanvasActive(bool isActive)
    {
        foreach (Transform child in inventoryCanvas.transform)
        {
            if (child.gameObject == itemSubtypeMenu)
            {
                // Keep the ItemSubtypeMenu and its children inactive
                continue;
            }

            child.gameObject.SetActive(isActive);
        }
    }
}
