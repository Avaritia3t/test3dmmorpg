using UnityEngine;
using UnityEngine.UI;

public class playerstatbarui : MonoBehaviour
{
    public Slider healthBar;
    public Slider shieldBar;
    public Camera mainCamera;
    public DomainControllerV2 playercontroller;

    void Awake()
    {
        Debug.LogError("npcstatbarui awake here");
        mainCamera = Camera.main;
        InitializeComponents();
    }

    void Start()
    {
        SetUpUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void InitializeComponents()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Debug.Log("playerstatcanvas found: " + (transform.Find("playerstatcanvas") != null));
        Debug.Log("PlayerHealthBarCanvas found: " + (transform.Find("playerstatcanvas/PlayerHealthBarCanvas") != null));
        Debug.Log("Healthbar found: " + (transform.Find("playerstatcanvas/PlayerHealthBarCanvas/Healthbar") != null));


        healthBar = transform.Find("playerstatcanvas/PlayerHealthBarCanvas/Healthbar").GetComponent<Slider>();
        shieldBar = transform.Find("playerstatcanvas/PlayerShieldBarCanvas/Shieldbar").GetComponent<Slider>();

        if (healthBar == null || shieldBar == null)
        {
            Debug.LogError("Failed to find one or both sliders on player stat canvas.");
        }

        // Assuming the CylinderControl script is attached to the parent object of this script
        playercontroller = GetComponentInParent<DomainControllerV2>();
    }
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
        else
        {
            mainCamera = Camera.main;  // Ensures a camera is always referenced if not set
        }
    }

    private void SetUpUI()
    {
        if (playercontroller != null)
        {
            // Log initial values to debug
            Debug.Log("Initial currentHP: " + playercontroller.currentHP);
            Debug.Log("Initial currentShield: " + playercontroller.currentShield);

            // Setting the maxValue for the sliders
            healthBar.maxValue = playercontroller.currentHP;
            shieldBar.maxValue = playercontroller.currentShield;

            // Log values after setting max
            Debug.Log("Assigned Max HealthBar Value: " + healthBar.maxValue);
            Debug.Log("Assigned Max ShieldBar Value: " + shieldBar.maxValue);

            // Ensure minValue is set correctly if needed
            healthBar.minValue = 0;  // Assuming the minimum health can be zero
            shieldBar.minValue = 0;  // Assuming the minimum shield can be zero

            // Set current values for the sliders
            healthBar.value = playercontroller.currentHP;
            shieldBar.value = playercontroller.currentShield;

            // Log values after assignment
            Debug.Log("Assigned HealthBar Value: " + healthBar.value);
            Debug.Log("Assigned ShieldBar Value: " + shieldBar.value);
        }
        else
        {
            Debug.LogError("PlayerController script not found on the parent object!");
        }

        // Dynamically set the size of the sliders
        float cylinderWidth = transform.parent.GetComponent<Renderer>().bounds.size.x;

        RectTransform healthBarRect = healthBar.GetComponent<RectTransform>();
        RectTransform shieldBarRect = shieldBar.GetComponent<RectTransform>();

        healthBarRect.localScale = new Vector3(0.01f, 0.01f, 1); // Setting x scaling
        shieldBarRect.localScale = new Vector3(0.01f, 0.01f, 1); // Setting x scaling

        healthBarRect.sizeDelta = new Vector2(100, 100); // Set width and height to 100
        shieldBarRect.sizeDelta = new Vector2(100, 100); // Set width and height to 100

        // Setting positions
        healthBarRect.anchoredPosition = new Vector3(0, 10, 0);
        shieldBarRect.anchoredPosition = new Vector3(0, 11, 0);
    }

    // Method to initialize the health and shield bars
    public void InitializeBars(float hp, float shield)
    {
        if (healthBar != null && shieldBar != null)
        {
            healthBar.maxValue = hp;
            healthBar.value = hp;
            shieldBar.maxValue = shield;
            shieldBar.value = shield;
        }
        else
        {
            Debug.LogError("Sliders not initialized properly.");
        }
    }

    private void UpdateUI()
    {
        // LookAtCameraBillboard();
        // RotateCanvas();
        UpdateHealthBar();
        UpdateShieldBar();
    }
    private void RotateCanvas()
    {
        Vector3 relativePos = new Vector3(mainCamera.transform.position.x, transform.position.y, mainCamera.transform.position.z) - transform.position;
        Quaternion rotation = Quaternion.LookRotation(relativePos);
        Quaternion horizontalRotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
        transform.rotation = horizontalRotation;
    }

    private void LookAtCameraBillboard()
    {
        Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
        transform.LookAt(targetPosition);
    }

    private void UpdateHealthBar()
    {
        // This can be modified to update as per game logic
        healthBar.value = playercontroller.currentHP;
    }

    private void UpdateShieldBar()
    {
        // This can be modified to update as per game logic
        shieldBar.value = playercontroller.currentShield;
    }
}
