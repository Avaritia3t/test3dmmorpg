using UnityEngine;
using UnityEngine.UI;

public class twoplayerstatbarui : MonoBehaviour
{
    public static twoplayerstatbarui Instance { get; private set; }
    
    public Slider healthBar;
    public Slider shieldBar;
    public float maxHealth;
    public float maxShield;

    private Transform target;
    private DomainControllerV3 playerController;

    private IPlayerStatsService _playerStatsService;
    private IPlayerStatsService PlayerStatsService => _playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Debug.LogError("2playerstatbarui awake here");

        // Initialize Sliders
        healthBar = GameObject.Find("PlayerHealthBar").GetComponent<Slider>();
        shieldBar = GameObject.Find("PlayerShieldBar").GetComponent<Slider>();

        // Check if Sliders are correctly initialized
        if (healthBar == null)
            Debug.LogError("Health Bar Slider is not found on the children objects!");
        else
            // Debug.Log("Health Bar Slider found.");

        if (shieldBar == null)
            Debug.LogError("Shield Bar Slider is not found on the children objects!");
        else
            // Debug.Log("Shield Bar Slider found.");

        // Set target and player controller
        target = GameObject.FindWithTag("Player").transform;
        
        if (target != null)
        {
            playerController = target.GetComponent<DomainControllerV3>();
            if (playerController == null)
                Debug.LogError("DomainControllerV3 component not found on target!");
            else
                PositionAboveTarget(target);
        }
        else
        {
            Debug.LogError("Target for 2playerstatbarui is not set.");
        }
    }

    void Start()
    {
        if (PlayerStatsService != null && PlayerStatsService.playerStats != null)
        {
            SetMaxHealth(PlayerStatsService.playerStats.baseHP);
            SetMaxShield(PlayerStatsService.playerStats.baseShield);
        }
        else
        {
            Debug.Log("IPlayerStatsService or playerStats is null.");
        }
    }

    void PositionAboveTarget(Transform target)
    {
        float yOffset = 10.0f;  // Offset to position above the player object
        Vector3 newPosition = new Vector3(target.position.x, target.position.y + yOffset, target.position.z);
        transform.position = newPosition;
        // Debug.Log($"Canvas positioned at {newPosition}");
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
