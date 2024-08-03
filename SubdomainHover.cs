using UnityEngine;

public class SubdomainHover : MonoBehaviour
{
    public Material hoverMaterial; // Assign via TerrainUtils when creating the sphere
    private Material originalMaterial;

    void Start()
    {
        originalMaterial = GetComponent<Renderer>().material; // Save the original material
    }

    void OnMouseEnter()
    {
        GetComponent<Renderer>().material = hoverMaterial; // Change to hover material on mouse enter
    }

    void OnMouseExit()
    {
        GetComponent<Renderer>().material = originalMaterial; // Revert to the original material on mouse exit
    }
}
