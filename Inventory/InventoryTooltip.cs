using UnityEngine;
using TMPro;

public class InventoryTooltip : MonoBehaviour
{
    public static InventoryTooltip Instance { get; private set; }

    public GameObject tooltipPanel;
    public TMP_Text tooltipText;
    public Vector3 offset; // Offset to position the tooltip slightly away from the cursor

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
        tooltipPanel.SetActive(false); // Hide the tooltip initially
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            Vector3 newPos = Input.mousePosition + offset;
            tooltipPanel.transform.position = newPos;
        }
    }

    public void ShowTooltip(string content)
    {
        tooltipText.text = content;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}
