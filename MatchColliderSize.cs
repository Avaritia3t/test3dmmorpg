using UnityEngine;

[ExecuteInEditMode] // This allows it to run in the editor
public class MatchColliderSize : MonoBehaviour
{
    void Start()
    {
        UpdateScaleToMatchCollider();
    }

    void UpdateScaleToMatchCollider()
    {
        BoxCollider boxCollider = GetComponentInParent<BoxCollider>();
        if (boxCollider != null)
        {
            Transform meshTransform = transform; // Assuming this script is on the mesh GameObject
            // Adjust scale to match the collider's size
            meshTransform.localScale = new Vector3(boxCollider.size.x, boxCollider.size.y, boxCollider.size.z);
            // Optional: Position adjustment if the collider is not centered
            meshTransform.localPosition = boxCollider.center;
        }
    }
}