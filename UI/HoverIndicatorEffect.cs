using UnityEngine;

/// <summary>
/// Raycast hover highlight for subdomain colliders. Assign <see cref="hoverIndicator"/> to the mesh to tint, or leave null to use this GameObject.
/// </summary>
public class HoverIndicatorEffect : MonoBehaviour
{
    public GameObject hoverIndicator;
    private Renderer indicatorRenderer;
    public Color defaultColor = new Color(1f, 0.415f, 0f);
    public Color hoverColor = new Color(0.137f, 0.078f, 0.898f);
    private bool isHovering = false;

    void Start()
    {
        if (hoverIndicator == null)
            hoverIndicator = gameObject;

        indicatorRenderer = hoverIndicator.GetComponent<Renderer>();
        if (indicatorRenderer != null)
        {
            indicatorRenderer.material = new Material(indicatorRenderer.material);
            indicatorRenderer.material.color = defaultColor;
            Debug.Log("Hover indicator activated and visible.");
        }
        else
            Debug.LogError("Renderer component missing on hoverIndicator.");
    }

    void Update()
    {
        if (indicatorRenderer == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("SubdomainColliders");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name);
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
