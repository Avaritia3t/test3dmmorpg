using UnityEngine;
using UnityEngine.AI;

public class DomainController : MonoBehaviour
{
    public NavMeshAgent agent;
    public string FactionName = "Heritage"; // player's faction name
    public float moveSpeed = 100f; // How fast the domain moves towards the target
    public float hoverHeight = 20f; // The height above the terrain
    private Vector3 targetPosition; // Target position to move towards
    // private bool firstUpdate = true;
    public bool mapBuffsApplied = false;

    void Awake()
    {
        Debug.Log($"[Awake] Position: {transform.position}");
    }

    void OnEnable()
    {
        Debug.Log($"[OnEnable] Position: {transform.position}");
    }
    void Start()
    {
        // LoadPosition(); // Load the last saved position at the start
        // Debug.Log($"Position after loading: {transform.position}");
        agent = GetComponent<NavMeshAgent>();
        agent.speed = 100f;
        agent.acceleration = 100f;
        Vector3 spawnPoint = MapManager.Instance.currentMap.spawnPoint;
        transform.position = spawnPoint;
        targetPosition = spawnPoint;
        ApplyMapBuffs();
        agent.acceleration = 100; // Set the acceleration to 100
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            SetTargetPosition();
            // targetSet = true;
        }

        // Debug.Log($"Current Move Speed: {moveSpeed}");
        
    }

    void SetTargetPosition()
    {
        Debug.Log($"Mouse Position (Screen): {Input.mousePosition}");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Attempt to hit the base terrain layer
        int baseTerrainLayerMask = LayerMask.GetMask("BaseTerrain");
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, baseTerrainLayerMask))
        {
            // Log the point on the base terrain that was clicked
            Debug.Log($"Base Terrain Hit Point: {hit.point}");

            // Calculate the corresponding point on the NavMesh layer above
            Vector3 navMeshPoint = new Vector3(hit.point.x, hit.point.y + 30, hit.point.z); // Assuming +30 units in Y direction to NavMesh layer

            // Optional: Use NavMesh.SamplePosition to find the nearest point on the NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(navMeshPoint, out navHit, 10.0f, NavMesh.AllAreas))
            {
                Debug.Log($"Translated NavMesh Point: {navHit.position}");
                agent.SetDestination(navHit.position);
            }
            else
            {
                Debug.Log("Failed to find a valid point on the NavMesh near the clicked location.");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit the BaseTerrain layer.");
        }
    }




    void OnApplicationQuit()
    {
        Debug.Log("Saving current position for next start...");
        SavePosition(); // Save the current position when the application quits
    }

    void SavePosition()
    {
        PlayerPrefs.SetFloat("DomainPositionX", transform.position.x);
        PlayerPrefs.SetFloat("DomainPositionY", transform.position.y);
        PlayerPrefs.SetFloat("DomainPositionZ", transform.position.z);
        PlayerPrefs.Save();
    }

    void LoadPosition()
    {
        float x = PlayerPrefs.GetFloat("DomainPositionX", 0);
        float y = PlayerPrefs.GetFloat("DomainPositionY", hoverHeight);
        float z = PlayerPrefs.GetFloat("DomainPositionZ", 0);

        Vector3 loadedPosition = new Vector3(x, y, z);
        transform.position = loadedPosition;
        targetPosition = loadedPosition; // Ensure targetPosition is updated to prevent movement towards 0,0,0

        Debug.Log($"Loaded position: {loadedPosition}");
    }

    public void ApplyMapBuffs()
    {
        if (!mapBuffsApplied)
        {
            MapManager.Instance.ApplyMapBuffs(gameObject); // Assuming MapManager is correctly set up
            mapBuffsApplied = true;
        }
    }

}
