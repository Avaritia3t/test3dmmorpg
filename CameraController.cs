using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform domain; // Reference to the domain object
    public float distance = 20f; // Initial distance from the domain for a more zoomed-out start
    public float zoomSpeed = 10f; // How quickly the camera zooms in/out
    public float rotationSpeed = 100f; // Speed of horizontal rotation
    public float pitchSpeed = 50f; // Speed of vertical pitch adjustments
    private float currentZoom;
    private float currentYaw = 0f;
    private float currentPitch = 45f; // Initial vertical angle

    void Start()
    {
        currentZoom = distance; // Ensures camera starts zoomed out as per 'distance' value.
    }

    void Update()
    {
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
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 position = rotation * new Vector3(0, 0, -currentZoom) + domain.position;

        transform.position = position;
        transform.LookAt(domain.position);
    }
}
