using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClassSelectionManager : MonoBehaviour
{
    public GameObject classSelectionPanel;
    public GameObject classDescriptionPanel;
    public TextMeshProUGUI classDescriptionText;
    public Button acceptButton;

    private Camera mainCamera;
    private GraphicRaycaster classSelectionRaycaster;
    private EventSystem eventSystem;
    private GameObject activeClassUI;
    private Vector3 originalPosition;
    private bool isHoveringOverClass = false;

    private List<GameObject> classUIObjects;
    private Dictionary<string, string> classDescriptions;

    public string selectedClassName;

    private IPlayerStatsService _playerStatsService;
    private ICanvasTransitionService _canvasTransitionService;
    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private ICanvasTransitionService CanvasTransitionService => _canvasTransitionService ??= GameBootstrap.Locator?.Get<ICanvasTransitionService>();

    void Start()
    {
        mainCamera = Camera.main;

        // Deactivate the Class Description Panel initially
        classDescriptionPanel.SetActive(false);

        // Initialize raycaster and event system
        classSelectionRaycaster = classSelectionPanel.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        // Collect all <classname>UI objects under classSelectionPanel
        classUIObjects = new List<GameObject>();
        foreach (Transform child in classSelectionPanel.transform)
        {
            if (child.name.EndsWith("UI"))
            {
                classUIObjects.Add(child.gameObject);
            }
        }

        classDescriptions = new Dictionary<string, string>
    {
        { "Entropia", "Sentient beings birthed during the collapse of a cosmic singularity, the Entropia are uncontrollable, indifferent super-beings motivated by a voracious, insatiable appetite to consume. Crackling, diamond-clad limbs of sentient energy stretch across galaxies, devouring peoples, worlds, and even each other in a never-ending role of conquest and annihilation." },
        { "Fortus", "The Fortus are the pallbearers of the light, those who have sworn to safeguard the birth of the next incarnation of the Universe with their own hands, those who have wrought destruction on a thousand worlds in service to the promise of a future. These paragons have sworn unbreakable oaths to hold vigil against the chaotic whims of the universe, and devote themselves to the disciplined, systematic extermination of chaotic phenomena in every plane of existence." },
        { "Principalis", "Whispers of the dread Principalia drift across the scoured wastelands of their domains, spoken in hushed, fearful voices for fear of execution and disassembly. The caste of grim, luxurious warlords rule their empires from abord massive glittering, floating citadels built on the backs of generations of enslaved, shipbound populations. The Principalia themselves are immortal ghasts, summoned to this plane through foul necromancy, leading hordes of fabricated undead to guide their living brethren into an eternity of rotting servitude." },
        { "Caeleria", "In the year 100001 of the Celestial calendar, the Caeleria surpassed the need for a physical form. Having shed their mortal coils, the Caeleria ricocheted through the universe at the speed of light, visiting worlds with the intent to spread their enlightenment, to welcome all of existence into the folds of glib, electric immortality. Each Caeleria traces their existence back to the Source, a collective consciousness they live in perpetual fear of losing on their lightspeed travels."},
        { "Vindictus", "Weary and wayworn, the Vindicta are those who have chosen to forego the safety of society in exchange for learning the natural ways of the spaces between the Void. Having cast off their morality, their allegiances, and any vestiges of a cooperative racial memory, the Vindicta have become tenaciously adept at thriving at the fringes of perception, and assembling resources in the emptiest of spaces. Each Vindictus lives only for themselves, interested only in continuing to exist, unbothered by mortal concepts of right and wrong." }
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
        if (classSelectionRaycaster == null)
        {
            classSelectionRaycaster = classSelectionPanel.GetComponent<GraphicRaycaster>();
            if (classSelectionRaycaster == null)
            {
                Debug.LogError("ClassSelectionRaycaster is null in HandleHover.");
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
        classSelectionRaycaster.Raycast(pointerEventData, results);

        bool isHoveringNow = false;
        GameObject hoveredClassImage = null;

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

                // Check if the object is a class image or the ClassDescriptionPanel
                if (hoveredObject.name.EndsWith("Image") || hoveredObject == classDescriptionPanel)
                {
                    isHoveringNow = true;
                    if (hoveredObject.name.EndsWith("Image"))
                    {
                        hoveredClassImage = hoveredObject;
                    }
                    break;
                }
            }
        }

        if (isHoveringNow && !isHoveringOverClass)
        {
            // Start hovering over a class image or description panel
            if (hoveredClassImage != null)
            {
                HandleClassSelection(hoveredClassImage);
            }
            isHoveringOverClass = true;
        }
        else if (!isHoveringNow && isHoveringOverClass)
        {
            // Stop hovering over a class image or description panel
            ResetClassSelection();
            isHoveringOverClass = false;
        }
    }

    private void HandleClassSelection(GameObject hoveredObject)
    {
        // Get the parent of the hovered image
        Transform classParent = hoveredObject.transform.parent;

        // Store the original position if not already stored
        if (activeClassUI == null)
        {
            originalPosition = classParent.localPosition;
        }

        // Move the parent object to the specified location
        classParent.localPosition = new Vector3(-705, 0, 0);

        // Set the class as the active class
        activeClassUI = classParent.gameObject;

        // Hide all other class UI objects
        foreach (var classUI in classUIObjects)
        {
            if (classUI != activeClassUI)
            {
                classUI.SetActive(false);
            }
        }

        // Activate and display the Class Description Panel
        classDescriptionPanel.SetActive(true);
        classDescriptionText.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(true);

        // Update the class description text
        string className = hoveredObject.name.Replace("Image", "");
        if (classDescriptions.TryGetValue(className, out string description))
        {
            classDescriptionText.text = $"{description}";
        }
        else
        {
            classDescriptionText.text = $"You have selected the {className} class.";
        }

        // Update the selected class name
        selectedClassName = className;
    }


    private void ResetClassSelection()
    {
        if (activeClassUI != null)
        {
            // Move the parent object back to its original position
            activeClassUI.transform.localPosition = originalPosition;

            // Reactivate all class UI objects
            foreach (var classUI in classUIObjects)
            {
                classUI.SetActive(true);
            }

            // Deactivate the Class Description Panel
            classDescriptionPanel.SetActive(false);
            classDescriptionText.gameObject.SetActive(false);
            acceptButton.gameObject.SetActive(false);

            // Reset the active class
            activeClassUI = null;
        }
    }

    private void OnAcceptButtonClick()
    {
        if (!string.IsNullOrEmpty(selectedClassName))
        {
            if (PlayerStatsService != null)
                PlayerStatsService.SetPlayerClass(selectedClassName);
            Debug.Log($"Class '{selectedClassName}' has been accepted.");

            // Notify the CanvasTransitionManager to move to the next stage
            CanvasTransitionService?.OnClassSelected();
        }
        else
        {
            Debug.LogWarning("No class selected!");
        }
    }

}
