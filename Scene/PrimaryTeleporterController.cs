using UnityEngine;

public class PrimaryTeleporterController : MonoBehaviour
{
    public float detectionRadius = 100f;
    public KeyCode teleportKey = KeyCode.J;

    private Renderer teleporterRenderer;
    private Color originalColor;
    private bool playerInRange = false;

    private void Start()
    {
        teleporterRenderer = GetComponent<Renderer>();
        originalColor = teleporterRenderer.material.color;
    }

    private void Update()
    {
        DetectPlayer();
        if (playerInRange && Input.GetKeyDown(teleportKey))
        {
            Teleport();
        }
    }

    private void DetectPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        bool playerDetected = false;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                playerDetected = true;
                if (!playerInRange)
                {
                    OnPlayerEnter();
                }
            }
        }

        if (!playerDetected && playerInRange)
        {
            OnPlayerExit();
        }
    }

    private void OnPlayerEnter()
    {
        playerInRange = true;
        teleporterRenderer.material.color = Color.yellow; // Highlight color
    }

    private void OnPlayerExit()
    {
        playerInRange = false;
        teleporterRenderer.material.color = originalColor;
    }

    private void Teleport()
    {
        // Placeholder for the actual teleport function
        Debug.Log("Teleport function executed");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
