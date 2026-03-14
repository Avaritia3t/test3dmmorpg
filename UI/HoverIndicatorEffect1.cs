using UnityEngine;

public class HoverIndicatorEffect : MonoBehaviour
{
    public GameObject hoverIndicator; // Assign this GameObject via the inspector if it's not this GameObject
    private Renderer indicatorRenderer;
    public Color defaultColor = new Color(1f, 0.415f, 0f); // Orange, Hex: #FF6A00
    public Color hoverColor = new Color(0.137f, 0.078f, 0.898f); // Blue, Hex: #2314E5
    private bool isHovering = false;

    void Start()
    {
        if (hoverIndicator == null)
        {
            hoverIndicator = gameObject; // Default to this GameObject if none is specified
        }

        indicatorRenderer = hoverIndicator.GetComponent<Renderer>();
        if (indicatorRenderer != null)
        {
            indicatorRenderer.material = new Material(indicatorRenderer.material); // Create a new instance to avoid shared material issues
            indicatorRenderer.material.color = defaultColor; // Set to default color on start
            Debug.Log("Hover indicator activated and visible.");
        }
        else
        {
            Debug.LogError("Renderer component missing on hoverIndicator.");
        }
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("SubdomainColliders"); // Only consider objects on the SubdomainColliders layer

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name); // Log the name of the hit object
            if (hit.collider.gameObject == hoverIndicator && !isHovering)
            {
                indicatorRenderer.material.color = hoverColor;
                isHovering = true;
                Debug.Log("Mouse entered, color changed to hover color.");
            }
        }
        else if (isHovering)
        {
            indicatorRenderer.material.color = defaultColor;
            isHovering = false;
            Debug.Log("Mouse exited, color reverted to default.");
        }
    }
}
