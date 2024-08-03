using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    public Vector2 offset = new Vector2(50, 100); // Offset for TooltipBox position

    public GameObject tooltipPanel; // TooltipPanel object
    private GameObject tooltipBox;   // TooltipBox object
    private TMP_Text tooltipText;    // TooltipText object

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure tooltipPanel is assigned
            if (tooltipPanel == null)
            {
                Debug.LogError("TooltipPanel is not assigned in TooltipManager.");
                return;
            }

            // Dynamically find and assign TooltipBox and TooltipText
            tooltipBox = tooltipPanel.transform.Find("TooltipBox").gameObject;
            tooltipText = tooltipBox.transform.Find("TooltipText").GetComponent<TMP_Text>();

            // Ensure the found components are valid
            if (tooltipBox == null || tooltipText == null)
            {
                Debug.LogError("TooltipBox or TooltipText could not be found within TooltipPanel.");
            }
        }
        else
        {
            Debug.LogWarning("Multiple instances of TooltipManager detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ensure the tooltip is hidden on game start
        HideTooltip();
    }

    private void Update()
    {
        // Update tooltipBox position to follow mouse cursor
        if (tooltipPanel.activeSelf && tooltipBox.activeSelf)
        {
            Vector2 mousePosition = Input.mousePosition;
            tooltipBox.transform.position = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, tooltipBox.transform.position.z);
            // Debug.Log($"Mouse Position: {mousePosition} | TooltipBox Position: {tooltipBox.transform.position}"); // Debug line
        }
    }

    public void ShowTooltip(string content)
    {
        if (tooltipPanel == null || tooltipText == null || tooltipBox == null)
        {
            Debug.LogError("TooltipPanel, TooltipText, or TooltipBox is not assigned in TooltipManager.");
            return;
        }

        // Debug.Log("Inside Showtooltip, setting tooltipPanel to true.");
        tooltipPanel.SetActive(true);
        tooltipBox.SetActive(true);

        // Ensure all children of tooltipPanel are set to active
        SetAllChildrenActive(tooltipPanel, true);

        tooltipText.text = content;
        // Debug.Log("Showing tooltip with content: " + content);
    }

    private void SetAllChildrenActive(GameObject parent, bool isActive)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }


    public void HideTooltip()
    {
        if (tooltipPanel == null || tooltipText == null || tooltipBox == null)
        {
            Debug.LogError("TooltipPanel, TooltipText, or TooltipBox is not assigned in TooltipManager.");
            return;
        }

        tooltipPanel.SetActive(false);
        tooltipBox.SetActive(false);
        tooltipText.text = "";
        // Debug.Log("Hiding tooltip");
    }
}
