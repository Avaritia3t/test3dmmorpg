using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Mirror;

[ExecuteInEditMode]
public class TerrainUtils : MonoBehaviour
{
    public Material sphereMaterial; // Material for the sphere, set in the Inspector
    public Material hoverMaterial;  // Material for hover effects, set in the Inspector
    public int NumberOfSubdomains = 20; // Default to 20, adjustable in Inspector
    private List<Vector3> subdomainCenters = new List<Vector3>(); // To store centers of existing subdomains
    public GameObject npcStatCanvasPrefab;

    [Tooltip("When set and running as server, subdomains are spawned via Mirror (server authority). Leave empty for local/single-player SubdomainV2 creation.")]
    public GameObject networkedSubdomainPrefab;

    private int npcCounter = 1; // Counter for naming NPCs sequentially

    void Start()
    {
        ClearSubdomains();
        CreateSubdomains(); // Create subdomains at runtime
    }

    [ContextMenu("Create Subdomains")]
    public void CreateSubdomains()
    {
        if (!Application.isPlaying)
        {
            ClearSubdomains();
        }

        if (networkedSubdomainPrefab != null && NetworkServer.active)
        {
            CreateSubdomainsServerNetworked();
            return;
        }
        if (networkedSubdomainPrefab != null && !NetworkServer.active)
        {
            // Clients do not create subdomains; server spawns and syncs them.
            return;
        }

        foreach (Transform child in transform)
        {
            Terrain terrain = child.GetComponent<Terrain>();
            if (terrain != null)
            {
                subdomainCenters.Clear();
                for (int i = 0; i < NumberOfSubdomains; i++)
                {
                    Vector3 randomPoint = CastRayForRandomPoint(terrain);
                    if (randomPoint != Vector3.zero && IsValidLocation(randomPoint, terrain))
                    {
                        GameObject subdomain = CreateSphereAtPoint(randomPoint, child);
                        subdomainCenters.Add(randomPoint);
                    }
                }
            }
        }
    }

    private void CreateSubdomainsServerNetworked()
    {
        foreach (Transform child in transform)
        {
            Terrain terrain = child.GetComponent<Terrain>();
            if (terrain == null) continue;
            subdomainCenters.Clear();
            for (int i = 0; i < NumberOfSubdomains; i++)
            {
                Vector3 randomPoint = CastRayForRandomPoint(terrain);
                if (randomPoint == Vector3.zero || !IsValidLocation(randomPoint, terrain)) continue;
                int level = Random.Range(1, 21);
                SubdomainV2Type type = NetworkedSubdomainSpawner.GetRandomSubdomainType();
                GameObject go = NetworkedSubdomainSpawner.ServerSpawnSubdomain(networkedSubdomainPrefab, randomPoint, Quaternion.identity, level, type);
                if (go != null)
                {
                    go.transform.SetParent(child);
                    subdomainCenters.Add(randomPoint);
                    npcCounter++;
                }
            }
        }
    }

    private Vector3 CastRayForRandomPoint(Terrain terrain)
    {
        // Debug.Log("CastRayForRandomPoint - TerrainUtils: Casting ray to find random point on terrain.");
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPosition = terrain.transform.position;
        float randomX = Random.Range(30, terrainSize.x - 30);
        float randomZ = Random.Range(30, terrainSize.z - 30);
        int terrainLayerMask = 1 << LayerMask.NameToLayer("BaseTerrain");
        Vector3 rayOrigin = new Vector3(terrainPosition.x + randomX, terrainPosition.y + terrainSize.y + 100, terrainPosition.z + randomZ);
        Vector3 rayDirection = Vector3.down;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, terrainSize.y + 200, terrainLayerMask))
        {
            // Debug.Log($"CastRayForRandomPoint - TerrainUtils: Raycast hit at {hit.point}");
            return hit.point;
        }
        // Debug.Log("CastRayForRandomPoint - TerrainUtils: Raycast did not hit.");
        return Vector3.zero;
    }

    private bool IsValidLocation(Vector3 point, Terrain terrain)
    {
        // Debug.Log($"IsValidLocation - TerrainUtils: Checking location validity at {point}");
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPosition = terrain.transform.position;

        // Edge distance validation adjusted for each terrain
        if (point.x <= terrainPosition.x + 30 || point.x >= terrainPosition.x + terrainSize.x - 30 ||
            point.z <= terrainPosition.z + 30 || point.z >= terrainPosition.z + terrainSize.z - 30)
        {
            // Debug.Log("IsValidLocation - TerrainUtils: Point is too close to the edge.");
            return false;
        }

        // Subdomain center proximity check
        foreach (Vector3 center in subdomainCenters)
        {
            if (Vector3.Distance(point, center) < 35)
            {
                // Debug.Log($"IsValidLocation - TerrainUtils: Point is too close to another center at {center}");
                return false;
            }
        }
        return true;
    }

    private GameObject CreateSphereAtPoint(Vector3 position, Transform parent)
    {
        // Debug.Log($"CreateSphereAtPoint - TerrainUtils: Creating sphere at {position}");

        string npcName = $"npc{npcCounter++.ToString("D3")}"; // Formats as npc001, npc002, etc.
        GameObject sphere = new GameObject(npcName);
        sphere.transform.position = position;
        sphere.transform.SetParent(parent);
        sphere.transform.localScale = new Vector3(Random.Range(12f, 25f), Random.Range(12f, 25f), Random.Range(12f, 25f));

        MeshFilter meshFilter = sphere.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateSphereMesh(); // Ensure this method returns a valid mesh
        // Debug.Log("MeshFilter and MeshRenderer added with mesh.");

        MeshRenderer renderer = sphere.AddComponent<MeshRenderer>();
        renderer.material = new Material(sphereMaterial); // Make sure sphereMaterial is set in the Inspector
        // Debug.Log("Material applied to MeshRenderer.");

        SphereCollider collider = sphere.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 1.0f;
        // Debug.Log("SphereCollider added and configured.");

        SubdomainV2 subdomain = sphere.AddComponent<SubdomainV2>(); // Change Subdomain V2 here or V whatever to control version.
        // Debug.Log("Subdomain component added to sphere.");

        sphere.layer = LayerMask.NameToLayer("SubdomainColliders");
        // Debug.Log("Layer set for the sphere.");

        GameObject statCanvas = Instantiate(npcStatCanvasPrefab, sphere.transform.position + Vector3.up * 27, Quaternion.identity, sphere.transform);
        // Debug.Log("NPC stat canvas prefab instantiated.");

        npcstatbarui statsUI = statCanvas.GetComponent<npcstatbarui>();

        if (statsUI != null)
        {
            // Debug.Log("npcstatbarui component found and will be set.");
            subdomain.SetStatsUI(statsUI);
            subdomain.CreateSubdomainStats();
            // Debug.LogError("Subdomain stats created and UI set.");
        }
        else
        {
            Debug.LogError("npcstatbarui component not found on the instantiated statCanvas!");
        }

        statCanvas.name = "NPCStatCanvas";
        // Debug.Log("StatCanvas named 'NPCStatCanvas'.");

        return sphere;
    }

    private Mesh CreateSphereMesh()
    {
        return Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
    }

    [ContextMenu("Clear Subdomains")]
    void ClearSubdomains()
    {
        // Debug.Log("ClearSubdomains - TerrainUtils: Clearing all subdomain spheres.");
        foreach (Transform child in transform)
        {
            List<GameObject> subdomainObjects = new List<GameObject>();
            foreach (Transform grandChild in child)
            {
                if (grandChild.gameObject.layer == LayerMask.NameToLayer("SubdomainColliders"))
                {
                    subdomainObjects.Add(grandChild.gameObject);
                }
            }
            foreach (GameObject subdomain in subdomainObjects)
            {
                DestroyImmediate(subdomain);
            }
        }
    }
}
