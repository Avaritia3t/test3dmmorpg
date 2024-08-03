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
            SubdomainV2 subdomain = hitObject.GetComponent<SubdomainV2>();
            if (subdomain != null)
            {
                HandleHoverObject(hitObject, subdomain);
            }
        }
        else
        {
            ResetHover();
        }

        if (isHovering && lastHoveredObject != null)
        {
            SubdomainV2 subdomain = lastHoveredObject.GetComponent<SubdomainV2>();
            if (subdomain != null)
            {
                DisplayStatsOnHover(subdomain);
            }
        }
    }

    private void HandleHoverObject(GameObject hitObject, SubdomainV2 subdomain)
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
        DisplayStatsOnHover(subdomain);
    }

    private void DisplayStatsOnHover(SubdomainV2 subdomain)
    {
        if (uiElement == null || subdomainText == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"Name: {subdomain.subdomainName}");
        sb.AppendLine($"Type: {subdomain.type}");
        sb.AppendLine($"Level: {subdomain.level}");
        sb.AppendLine($"State: {subdomain.CurrentState}");
        sb.AppendLine($"Is Generating Items and Resources: {subdomain.IsGeneratingItemsAndResources}");
        sb.AppendLine($"HP: {subdomain.currentHP} / {subdomain.SubdomainHP}");
        sb.AppendLine($"Shield: {subdomain.currentShield} / {subdomain.SubdomainShield}");
        sb.AppendLine("Inventory:");

        if (subdomain.resources.Count > 0)
        {
            sb.AppendLine("Resources:");
            foreach (var resource in subdomain.resources)
            {
                sb.AppendLine($"- {resource.type}: {resource.quantity} ({resource.grade})");
            }
        }
        else
        {
            sb.AppendLine("No resources in inventory.");
        }

        if (subdomain.spawnedItems.Count > 0)
        {
            sb.AppendLine("Items:");
            foreach (var item in subdomain.spawnedItems)
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
