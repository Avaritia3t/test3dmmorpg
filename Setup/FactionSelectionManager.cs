using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FactionSelectionManager : MonoBehaviour
{
    public GameObject factionSelectionPanel;
    public GameObject factionDescriptionPanel;
    public TextMeshProUGUI factionDescriptionText;
    public Button acceptButton;

    private Camera mainCamera;
    private GraphicRaycaster factionSelectionRaycaster;
    private EventSystem eventSystem;
    private GameObject activeFactionUI;
    private Vector3 originalPosition;
    private bool isHoveringOverFaction = false;

    private List<GameObject> factionUIObjects;
    private Dictionary<string, string> factionDescriptions;
    private string selectedFactionName;

    void Start()
    {
        mainCamera = Camera.main;

        // Deactivate the Faction Description Panel initially
        factionDescriptionPanel.SetActive(false);

        // Initialize raycaster and event system
        factionSelectionRaycaster = factionSelectionPanel.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        // Collect all <factionname>UI objects under factionSelectionPanel
        factionUIObjects = new List<GameObject>();
        foreach (Transform child in factionSelectionPanel.transform)
        {
            if (child.name.EndsWith("UI"))
            {
                factionUIObjects.Add(child.gameObject);
            }
        }

        // Initialize faction descriptions
        factionDescriptions = new Dictionary<string, string>
        {
            { "Heritage", "Heritage is steeped in tradition, valuing the old ways and ancestral power." },
            { "Publicus", "Publicus represents the voice of the people, striving for equality and fairness." },
            { "Nadiria", "Nadiria dwells in the shadows, embracing secrecy and cunning strategies." },
            { "Lapsu", "Lapsu seeks redemption, a faction of fallen warriors looking for a second chance." },
            { "Forsaken", "Forsaken is the embodiment of the outcasts, those who have been rejected by society." }
        };

        // Add a listener to the Accept button
        acceptButton.onClick.AddListener(OnAcceptButtonClick);
    }

    void Update()
    {
        HandleHover();
    }

    private void HandleHover()
    {
        if (factionSelectionRaycaster == null)
        {
            factionSelectionRaycaster = factionSelectionPanel.GetComponent<GraphicRaycaster>();
            if (factionSelectionRaycaster == null)
            {
                Debug.LogError("FactionSelectionRaycaster is null in HandleHover.");
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
        factionSelectionRaycaster.Raycast(pointerEventData, results);

        bool isHoveringNow = false;
        GameObject hoveredFactionImage = null;

        if (results.Count > 0)
        {
            foreach (RaycastResult result in results)
            {
                GameObject hoveredObject = result.gameObject;

                // Exclude objects with "Background" in their name
                if (hoveredObject.name.Contains("Background"))
                {
                    continue;
                }

                // Check if the object is a faction image or the FactionDescriptionPanel
                if (hoveredObject.name.EndsWith("Image") || hoveredObject == factionDescriptionPanel)
                {
                    isHoveringNow = true;
                    if (hoveredObject.name.EndsWith("Image"))
                    {
                        hoveredFactionImage = hoveredObject;
                    }
                    break;
                }
            }
        }

        if (isHoveringNow && !isHoveringOverFaction)
        {
            // Start hovering over a faction image or description panel
            if (hoveredFactionImage != null)
            {
                HandleFactionSelection(hoveredFactionImage);
            }
            isHoveringOverFaction = true;
        }
        else if (!isHoveringNow && isHoveringOverFaction)
        {
            // Stop hovering over a faction image or description panel
            ResetFactionSelection();
            isHoveringOverFaction = false;
        }
    }

    private void HandleFactionSelection(GameObject hoveredObject)
    {
        // Get the parent of the hovered image
        Transform factionParent = hoveredObject.transform.parent;

        // Store the original position if not already stored
        if (activeFactionUI == null)
        {
            originalPosition = factionParent.localPosition;
        }

        // Move the parent object to the specified location
        factionParent.localPosition = new Vector3(-705, 0, 0);

        // Set the faction as the active faction
        activeFactionUI = factionParent.gameObject;

        // Hide all other faction UI objects
        foreach (var factionUI in factionUIObjects)
        {
            if (factionUI != activeFactionUI)
            {
                factionUI.SetActive(false);
            }
        }

        // Activate and display the Faction Description Panel
        factionDescriptionPanel.SetActive(true);
        factionDescriptionText.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(true);

        // Update the faction description text
        string factionName = hoveredObject.name.Replace("Image", "");
        selectedFactionName = factionName; // Store the selected faction name
        if (factionDescriptions.TryGetValue(factionName, out string description))
        {
            factionDescriptionText.text = $"You have selected the {factionName} faction.\n\n{description}";
        }
        else
        {
            factionDescriptionText.text = $"You have selected the {factionName} faction.";
        }
    }

    private void ResetFactionSelection()
    {
        if (activeFactionUI != null)
        {
            // Move the parent object back to its original position
            activeFactionUI.transform.localPosition = originalPosition;

            // Reactivate all faction UI objects
            foreach (var factionUI in factionUIObjects)
            {
                factionUI.SetActive(true);
            }

            // Deactivate the Faction Description Panel
            factionDescriptionPanel.SetActive(false);
            factionDescriptionText.gameObject.SetActive(false);
            acceptButton.gameObject.SetActive(false);

            // Reset the active faction
            activeFactionUI = null;
            selectedFactionName = null;
        }
    }

    private void OnAcceptButtonClick()
    {
        if (!string.IsNullOrEmpty(selectedFactionName))
        {
            PlayerStatsManager.Instance.SetPlayerFaction(selectedFactionName);
            Debug.Log($"Faction '{selectedFactionName}' has been accepted.");

            // Notify the CanvasTransitionManager to move to the next stage
            CanvasTransitionManager.Instance.OnFactionSelected();
        }
        else
        {
            Debug.LogWarning("No faction selected!");
        }
    }

}
