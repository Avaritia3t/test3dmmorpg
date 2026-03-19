using UnityEngine;
using Mirror;

/// <summary>
/// Third-person camera that follows the local player. Uses NetworkClient.localPlayer when Mirror is active.
/// Required: one instance in scene (e.g. on main camera or a camera rig). No component requirements on player.
/// </summary>
public class NetworkedCameraController : MonoBehaviour
{
    public Transform domain;
    public float distance = 20f;
    public float zoomSpeed = 10f;
    public float rotationSpeed = 100f;
    public float pitchSpeed = 50f;
    private float currentZoom;
    private float currentYaw = 0f;
    private float currentPitch = 45f;

    void Start()
    {
        currentZoom = distance;
        AssignLocalPlayerTarget();
    }

    void OnEnable()
    {
        if (domain == null)
            AssignLocalPlayerTarget();
    }

    private void AssignLocalPlayerTarget()
    {
        if (domain != null) return;
        if (NetworkClient.active && NetworkClient.localPlayer != null)
        {
            domain = NetworkClient.localPlayer.transform;
            return;
        }
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            domain = playerObject.transform;
        else
            Debug.LogError("[NetworkedCameraController] Player not found. Tag 'Player' or Mirror localPlayer.");
    }

    void Update()
    {
        if (domain == null)
        {
            AssignLocalPlayerTarget();
            return;
        }
        ControlZoom();
        ControlRotation();
        ControlPitch();
    }

    void LateUpdate()
    {
        ApplyCameraTransform();
    }

    void ControlZoom()
    {
        currentZoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, 10f, 100f); // Now allows further zooming out.
    }

    void ControlRotation()
    {
        if (Input.GetKey(KeyCode.A)) currentYaw += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) currentYaw -= rotationSpeed * Time.deltaTime;
    }

    void ControlPitch()
    {
        if (Input.GetKey(KeyCode.W)) currentPitch += pitchSpeed * Time.deltaTime; // Adjusted for intuitive control
        if (Input.GetKey(KeyCode.S)) currentPitch -= pitchSpeed * Time.deltaTime; // Adjusted for intuitive control
        currentPitch = Mathf.Clamp(currentPitch, 10, 80); // Prevents flipping over
    }

    void ApplyCameraTransform()
    {
        if (domain == null) return;

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 position = rotation * new Vector3(0, 0, -currentZoom) + domain.position;

        transform.position = position;
        transform.LookAt(domain.position);
    }
}
