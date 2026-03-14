using UnityEngine;
using UnityEngine.UI;

public class npcstatbarui : MonoBehaviour
{
    public Slider healthBar;
    public Slider shieldBar;
    public Transform target; // NPC Transform the UI should follow
    public Camera mainCamera;

    void Awake()
    {
        // Debug.LogError("npcstatbarui awake here");
        InitializeSliders();
    }

    void Start()
    {
        if (target != null)
        {
            PositionAboveTarget(); // Set position above the target at start
            SetupHealthBar();
            SetupShieldBar();

            // Deactivate the canvas or container after setup
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Target for npcstatbarui is not set.");
        }
    }
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
        else
        {
            mainCamera = Camera.main;  // Ensures a camera is always referenced if not set
        }
    }


    void InitializeSliders()
    {
        healthBar = transform.Find("NPCHealthBar/Healthbar").GetComponent<Slider>();
        shieldBar = transform.Find("NPCShieldBar/Shieldbar").GetComponent<Slider>();

        if (healthBar == null)
            Debug.LogError("Health Bar Slider is not found on the children objects!");
        if (shieldBar == null)
            Debug.LogError("Shield Bar Slider is not found on the children objects!");
    }

    void PositionAboveTarget()
    {
        float yOffset = 3.0f;  // Offset to position above the NPC
        Vector3 newPosition = new Vector3(target.position.x, target.position.y + yOffset, target.position.z);
        transform.position = newPosition;
        // Debug.Log($"Canvas positioned at {newPosition}");
    }

    void SetupHealthBar()
    {
        if (healthBar != null)
        {
            // Access the parent's RectTransform
            RectTransform parentRect = healthBar.transform.parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // Debug.Log($"[SetupHealthBar] Initial Health Bar Parent Position: {parentRect.anchoredPosition}, Initial Scale: {parentRect.localScale}");

                parentRect.anchoredPosition = Vector2.zero; // Centers the health bar's parent relative to its parent
                parentRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Sets scale of the parent

                // Debug.Log($"[SetupHealthBar] Updated Health Bar Parent Position: {parentRect.anchoredPosition}, Updated Scale: {parentRect.localScale}");
            }
            else
            {
                Debug.LogError("[SetupHealthBar] RectTransform component not found on Health Bar's parent.");
            }
        }
        else
        {
            Debug.LogError("[SetupHealthBar] HealthBar is not assigned.");
        }
    }


    void SetupShieldBar()
    {
        if (shieldBar != null)
        {
            // Access the parent's RectTransform
            RectTransform parentRect = shieldBar.transform.parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // Debug.Log($"[SetupShieldBar] Initial Shield Bar Parent Position: {parentRect.anchoredPosition}, Initial Scale: {parentRect.localScale}");

                parentRect.anchoredPosition = new Vector2(0f, 0.1f); // Sets position above the health bar's parent
                parentRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Sets scale of the parent

                // Debug.Log($"[SetupShieldBar] Updated Shield Bar Parent Position: {parentRect.anchoredPosition}, Updated Scale: {parentRect.localScale}");
            }
            else
            {
                Debug.LogError("[SetupShieldBar] RectTransform component not found on Shield Bar's parent.");
            }
        }
        else
        {
            Debug.LogError("[SetupShieldBar] ShieldBar is not assigned.");
        }
    }


    public void SetMaxHealth(float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = maxHealth;
            // Debug.Log($"SetMaxHealth: Health Bar max value set to {maxHealth}");
        }
        else
        {
            Debug.LogError("Attempted to set max health but Health Bar is null!");
        }
    }

    public void SetMaxShield(float maxShield)
    {
        if (shieldBar != null)
        {
            shieldBar.maxValue = maxShield;
            shieldBar.value = maxShield;
            // Debug.Log($"SetMaxShield: Shield Bar max value set to {maxShield}");
        }
        else
        {
            Debug.LogError("Attempted to set max shield but Shield Bar is null!");
        }
    }

    public void UpdateHealth(float currentHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            // Debug.Log($"UpdateHealth: Health Bar current value updated to {currentHealth}");
        }
        else
        {
            Debug.LogError("Attempted to update health but Health Bar is null!");
        }
    }

    public void UpdateShield(float currentShield)
    {
        if (shieldBar != null)
        {
            shieldBar.value = currentShield;
            // Debug.Log($"UpdateShield: Shield Bar current value updated to {currentShield}");
        }
        else
        {
            Debug.LogError("Attempted to update shield but Shield Bar is null!");
        }
    }
}
