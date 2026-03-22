using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class HoverManagerV2 : MonoBehaviour
{
    public static HoverManagerV2 Instance { get; private set; }

    public Camera mainCamera; // Assign this via the Inspector or automatically
    public Material hoverMaterial; // Hover material, assign in the inspector
    public GameObject uiElement; // UI element that will display the stats
    public TextMeshProUGUI subdomainText; // Text component of the UI element, link via editor
    private bool isHovering = false;

    private GameObject lastHoveredObject;
    private Material lastOriginalMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        uiElement = GameObject.FindGameObjectWithTag("SubdomainUI");
        if (uiElement != null)
        {
            subdomainText = uiElement.GetComponentInChildren<TextMeshProUGUI>();
            if (subdomainText == null)
            {
                Debug.LogError("Text component not found in the SubdomainUI panel.");
            }
        }
        else
        {
            Debug.LogError("SubdomainUI panel not found. Please ensure it's tagged correctly.");
        }
        uiElement?.SetActive(false);
    }

    void Update()
    {
        // Ensure the main camera is assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Automatically assigns the main camera in the scene
            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera is missing and could not be assigned.");
                return; // Exit the Update method if the camera is missing
            }
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("SubdomainColliders");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            var netSub = hitObject.GetComponent<NetworkedSubdomainController>();
            var legacySub = hitObject.GetComponent<SubdomainV2>();
            if (netSub != null || legacySub != null)
                HandleHoverObject(hitObject, netSub, legacySub);
        }
        else
        {
            ResetHover();
        }

        if (isHovering && lastHoveredObject != null)
        {
            var netSub = lastHoveredObject.GetComponent<NetworkedSubdomainController>();
            var legacySub = lastHoveredObject.GetComponent<SubdomainV2>();
            if (netSub != null || legacySub != null)
                DisplayStatsOnHover(netSub, legacySub);
        }
    }

    private void HandleHoverObject(GameObject hitObject, NetworkedSubdomainController netSub, SubdomainV2 legacySub)
    {
        if (lastHoveredObject == hitObject)
            return;

        ResetHover();
        lastHoveredObject = hitObject;
        lastOriginalMaterial = hitObject.GetComponent<Renderer>().material;
        hitObject.GetComponent<Renderer>().material = hoverMaterial;

        var statUI = hitObject.GetComponentInChildren<npcstatbarui>(true);
        if (statUI != null)
        {
            statUI.gameObject.SetActive(true);
        }

        isHovering = true;
        DisplayStatsOnHover(netSub, legacySub);
    }

    /// <summary>Mirror server-spawned subdomains use <see cref="NetworkedSubdomainController"/>; offline spheres use <see cref="SubdomainV2"/>.</summary>
    private void DisplayStatsOnHover(NetworkedSubdomainController netSub, SubdomainV2 legacySub)
    {
        if (uiElement == null || subdomainText == null)
            return;

        string name = netSub != null ? netSub.subdomainName : legacySub.subdomainName;
        var type = netSub != null ? netSub.type : legacySub.type;
        int level = netSub != null ? netSub.level : legacySub.level;
        object state = netSub != null ? (object)netSub.CurrentState : legacySub.CurrentState;
        bool generating = netSub != null ? netSub.IsGeneratingItemsAndResources : legacySub.IsGeneratingItemsAndResources;
        float curHp = netSub != null ? netSub.currentHP : legacySub.currentHP;
        float maxHp = netSub != null ? netSub.SubdomainHP : legacySub.SubdomainHP;
        float curSh = netSub != null ? netSub.currentShield : legacySub.currentShield;
        float maxSh = netSub != null ? netSub.SubdomainShield : legacySub.SubdomainShield;
        var resList = netSub != null ? netSub.resources : legacySub.resources;
        var itemsList = netSub != null ? netSub.spawnedItems : legacySub.spawnedItems;

        var sb = new StringBuilder();
        sb.AppendLine($"Name: {name}");
        sb.AppendLine($"Type: {type}");
        sb.AppendLine($"Level: {level}");
        sb.AppendLine($"State: {state}");
        sb.AppendLine($"Is Generating Items and Resources: {generating}");
        sb.AppendLine($"HP: {curHp} / {maxHp}");
        sb.AppendLine($"Shield: {curSh} / {maxSh}");
        sb.AppendLine("Inventory:");

        if (resList != null && resList.Count > 0)
        {
            sb.AppendLine("Resources:");
            foreach (var resource in resList)
            {
                sb.AppendLine($"- {resource.type}: {resource.quantity} ({resource.grade})");
            }
        }
        else
        {
            sb.AppendLine("No resources in inventory.");
        }

        if (itemsList != null && itemsList.Count > 0)
        {
            sb.AppendLine("Items:");
            foreach (var item in itemsList)
            {
                sb.AppendLine($"- {item.itemName} ({item.itemRarity})");
                foreach (var stat in item.stats)
                {
                    sb.AppendLine($"  - {stat.statName}: {stat.statValue}");
                }
            }
        }
        else
        {
            sb.AppendLine("No items in inventory.");
        }

        subdomainText.text = sb.ToString();
        uiElement.SetActive(true);
    }

    private void ResetHover()
    {
        if (lastHoveredObject == null)
            return;

        lastHoveredObject.GetComponent<Renderer>().material = lastOriginalMaterial;

        var statUI = lastHoveredObject.GetComponentInChildren<npcstatbarui>(true);
        if (statUI != null)
        {
            statUI.gameObject.SetActive(false);
        }

        uiElement.SetActive(false);
        lastHoveredObject = null;
        isHovering = false;
    }
}
