using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NetworkedDomainController : MonoBehaviour
{
    // Movement
    private NavMeshAgent agent;
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float acceleration = 100f;

    // State
    private bool mapBuffsApplied;
    private float lastAttackedTime;
    private Coroutine regenerateCoroutine;

    // Combat (delegated to helper)
    private NetworkedPlayerCombatHelperController combatHelper;

    // Dependencies (external access for map buffs / spawn)
    public PlayerStatsManager playerStatsManager;
    public string factionName = "Heritage";

    // UI and camera (resolved at runtime)
    private twoplayerstatbarui playerHealthUi;
    private Camera mainCamera;

    // Services
    private IPlayerStatsService playerStatsService;
    private IMapService mapService;
    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private IMapService MapService => mapService ??= GameBootstrap.Locator?.Get<IMapService>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        agent = GetComponent<NavMeshAgent>();
        playerStatsManager = GetComponent<PlayerStatsManager>();
        combatHelper = GetComponent<NetworkedPlayerCombatHelperController>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.acceleration = acceleration;
        }
        else
        {
            Debug.LogError("[NetworkedDomainController] NavMeshAgent not found on this object.");
        }

        if (combatHelper == null)
            Debug.LogWarning("[NetworkedDomainController] NetworkedPlayerCombatHelperController not found. Combat input will be no-op.");

        InitializePlayerStatCanvas();
        InitializeAgent();
        ApplyMapBuffs();
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[NetworkedDomainController] Main Camera not found in the scene.");
                return;
            }
        }

        CheckForMouseInput();
        CheckForKeyboardInput();
        TickCombat();
    }

    private void CheckForMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject hoveredObject = GetHoveredObject();
            if (hoveredObject != null && combatHelper != null)
                combatHelper.TrySetTargetFromHover(hoveredObject);
        }

        if (Input.GetMouseButton(0))
            ClickToMove();
    }

    private void CheckForKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && combatHelper != null)
            combatHelper.ToggleAttacking();
    }

    private void TickCombat()
    {
        combatHelper?.Tick();
    }

    /// <summary>
    /// Raycast from current mouse position; returns the hit object if it is on a targetable layer (e.g. SubdomainColliders), otherwise null.
    /// </summary>
    private GameObject GetHoveredObject()
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        LayerMask layerMask = ~LayerMask.GetMask("NavigationTerrain");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            GameObject target = hit.collider.gameObject;
            if (target != null && target.layer == LayerMask.NameToLayer("SubdomainColliders"))
                return target;
        }
        return null;
    }

    /// <summary>
    /// Resolves and caches the player stat canvas UI. Call from Awake so health/shield updates have a valid reference.
    /// </summary>
    private void InitializePlayerStatCanvas()
    {
        GameObject canvasObj = GameObject.Find("playerstatcanvas");
        if (canvasObj == null)
        {
            Debug.LogError("[NetworkedDomainController] playerstatcanvas not found in scene.");
            return;
        }

        playerHealthUi = canvasObj.GetComponent<twoplayerstatbarui>();
        if (playerHealthUi == null)
            Debug.LogError("[NetworkedDomainController] twoplayerstatbarui component not found on playerstatcanvas.");
        // Debug.Log("[NetworkedDomainController] Attempting to access the twoplayerstatbarui script");
    }

    private void InitializeAgent()
    {
        if (MapService == null || MapService.currentMap == null)
        {
            Debug.LogError("[NetworkedDomainController] IMapService or currentMap is null.");
            return;
        }

        transform.position = MapService.currentMap.spawnPoint;
    }

    private void ClickToMove()
    {
        if (mainCamera == null || agent == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int baseTerrainLayerMask = LayerMask.GetMask("BaseTerrain");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, baseTerrainLayerMask))
        {
            Vector3 navMeshPoint = new Vector3(hit.point.x, hit.point.y + 30f, hit.point.z);

            if (NavMesh.SamplePosition(navMeshPoint, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
                agent.SetDestination(navHit.position);
            // else
            //     Debug.Log("[NetworkedDomainController] Failed to find a valid point on the NavMesh near the clicked location.");
        }
        // Debug.Log("Raycast did not hit the BaseTerrain layer.");
    }

    /// <summary>Returns this player's current HP for aggro/target checks (e.g. subdomain retaliation).</summary>
    public float GetCurrentHP()
    {
        if (PlayerStatsService == null) return 0f;
        return PlayerStatsService.playerStats.currentHP;
    }

    public void TakeDamage(float hpDamage, float shieldDamage)
    {
        if (PlayerStatsService == null) return;

        PlayerStats playerStats = PlayerStatsService.playerStats;
        lastAttackedTime = Time.time;

        // Debug.Log($"NetworkedDomainController TakeDamage: Initial Shield = {playerStats.currentShield}, Initial HP = {playerStats.currentHP}");

        if (playerStats.currentShield > 0)
        {
            float shieldDepletion = Mathf.Min(shieldDamage, playerStats.currentShield);
            playerStats.currentShield -= shieldDepletion;
            float excessShieldDamage = shieldDamage - shieldDepletion;
            playerStats.currentHP -= (hpDamage + excessShieldDamage);

            if (playerHealthUi != null)
            {
                playerHealthUi.UpdateShield(playerStats.currentShield);
                playerHealthUi.UpdateHealth(playerStats.currentHP);
            }
            // Debug.Log($"NetworkedDomainController TakeDamage: Shield/HP after damage. Shield = {playerStats.currentShield}, HP = {playerStats.currentHP}");
        }
        else
        {
            playerStats.currentHP -= hpDamage;
            if (playerHealthUi != null)
                playerHealthUi.UpdateHealth(playerStats.currentHP);
            // Debug.Log($"NetworkedDomainController TakeDamage: No Shield, HP after damage = {playerStats.currentHP}");
        }

        if (playerStats.currentHP <= 0)
        {
            HandleDeath();
            playerStats.currentHP = 0;
            if (playerHealthUi != null)
                playerHealthUi.UpdateHealth(playerStats.currentHP);
            // Debug.Log("NetworkedDomainController TakeDamage: Player has died. HP set to 0.");
        }

        if (regenerateCoroutine != null)
            StopCoroutine(regenerateCoroutine);

        regenerateCoroutine = StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        if (PlayerStatsService == null) yield break;

        PlayerStats playerStats = PlayerStatsService.playerStats;
        yield return new WaitForSeconds(playerStats.combatRegenDelay);

        while (playerStats.currentHP < playerStats.baseHP || playerStats.currentShield < playerStats.baseShield)
        {
            if (Time.time - lastAttackedTime < playerStats.combatRegenDelay)
            {
                regenerateCoroutine = null;
                yield break;
            }

            playerStats.currentHP += playerStats.baseHP * (playerStats.hpRegenPercent / 100f) * Time.deltaTime;
            playerStats.currentShield += playerStats.baseShield * (playerStats.shieldRegenPercent / 100f) * Time.deltaTime;
            playerStats.currentHP = Mathf.Min(playerStats.currentHP, playerStats.baseHP);
            playerStats.currentShield = Mathf.Min(playerStats.currentShield, playerStats.baseShield);

            if (playerHealthUi != null)
            {
                playerHealthUi.UpdateHealth(playerStats.currentHP);
                playerHealthUi.UpdateShield(playerStats.currentShield);
            }

            yield return null;
        }

        regenerateCoroutine = null;
    }

    private void HandleDeath()
    {
        // Debug.Log("Unit has died.");
    }

    public void ApplyMapBuffs()
    {
        if (mapBuffsApplied) return;
        if (MapService != null)
        {
            MapService.ApplyMapBuffs(gameObject);
            mapBuffsApplied = true;
        }
    }
}
