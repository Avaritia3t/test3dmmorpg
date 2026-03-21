using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// Player controller for networked games: movement (local input), map buffs, combat via NetworkedPlayerCombatHelperController.
/// Required: on player prefab, with NetworkIdentity, SyncPlayerStats, NetworkedPlayerCombatHelperController, NavMeshAgent, PlayerStatsManager.
/// Mirror + ParrelSync: local NavMesh click-to-move + NetworkTransform (see <see cref="NetworkedPlayerMovement"/> when enabling strict server movement).
/// Optional: NetworkedPlayerLootReceiver for receiving server-routed loot.
/// </summary>
public class NetworkedDomainController : MonoBehaviour
{
    // Movement
    private NavMeshAgent agent;
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float acceleration = 100f;
    private float statusMoveSpeedMultiplier = 1f;
    private bool statusMovementRooted = false;

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

    // Mirror: optional; when present, input is local-player-only and stats are server-synced
    private NetworkIdentity networkIdentity;
    private SyncPlayerStats syncPlayerStats;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerStatsManager = GetComponent<PlayerStatsManager>();
        combatHelper = GetComponent<NetworkedPlayerCombatHelperController>();
        networkIdentity = GetComponent<NetworkIdentity>();
        syncPlayerStats = GetComponent<SyncPlayerStats>();

        if (agent != null)
        {
            agent.acceleration = acceleration;
            ApplyMoveSpeed();
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
        if (networkIdentity == null || networkIdentity.isLocalPlayer)
            DontDestroyOnLoad(gameObject);
        if (syncPlayerStats != null && networkIdentity != null && NetworkServer.active && playerStatsManager != null)
        {
            syncPlayerStats.ServerInitFrom(playerStatsManager.playerStats);
            syncPlayerStats.ServerSetMoveSpeed(GetServerMoveSpeed());
        }
    }

    private void Update()
    {
        if (networkIdentity != null && !networkIdentity.isLocalPlayer)
        {
            if (NetworkServer.active)
            {
                ApplyMoveSpeed();
                TickCombat();
            }
            return;
        }

        ApplyMoveSpeed();

        if (syncPlayerStats != null && playerHealthUi != null)
        {
            playerHealthUi.UpdateHealth(syncPlayerStats.currentHP);
            playerHealthUi.UpdateShield(syncPlayerStats.currentShield);
        }

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

    /// <summary>Server: compute move speed from server-side stats and sync to clients. Client: apply server's syncMoveSpeed.</summary>
    private void ApplyMoveSpeed()
    {
        if (agent == null) return;

        if (NetworkServer.active)
        {
            // Server-authoritative: compute from server's state only (map buffs applied at spawn; future: equipment/combat effects)
            float speed = GetServerMoveSpeed();

            float effectiveSpeed = statusMovementRooted ? 0f : speed * Mathf.Max(0f, statusMoveSpeedMultiplier);
            agent.speed = effectiveSpeed;
            if (syncPlayerStats != null)
                syncPlayerStats.ServerSetMoveSpeed(effectiveSpeed);
        }
        else
        {
            // Client: use server-synced value only (no client-side override)
            float speed = syncPlayerStats != null && syncPlayerStats.syncMoveSpeed > 0f
                ? syncPlayerStats.syncMoveSpeed
                : moveSpeed;
            agent.speed = speed;
        }
    }

    /// <summary>Server only: move speed from server's PlayerStats (map buffs, etc.). Fallback to serialized default.</summary>
    private float GetServerMoveSpeed()
    {
        if (playerStatsManager != null && playerStatsManager.playerStats.moveSpeed > 0f)
            return playerStatsManager.playerStats.moveSpeed;
        return moveSpeed;
    }

    /// <summary>Called by server-side status effects to apply slow/root movement modifiers.</summary>
    public void SetMovementRootedAndSlow(bool rooted, float slowMultiplier)
    {
        statusMovementRooted = rooted;
        statusMoveSpeedMultiplier = Mathf.Max(0f, slowMultiplier);

        // Root should stop path following immediately.
        if (agent != null)
            agent.isStopped = rooted;
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
        if (networkIdentity != null)
            return;
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
        }
    }

    /// <summary>Returns this player's current HP for aggro/target checks (e.g. subdomain retaliation).</summary>
    public float GetCurrentHP()
    {
        if (syncPlayerStats != null)
            return syncPlayerStats.currentHP;
        if (playerStatsManager == null) return 0f;
        return playerStatsManager.playerStats.currentHP;
    }

    public void TakeDamage(float hpDamage, float shieldDamage)
    {
        if (networkIdentity != null && !NetworkServer.active)
            return;
        if (playerStatsManager == null) return;

        float damageMagnitude = Mathf.Abs(hpDamage) + Mathf.Abs(shieldDamage);

        PlayerStats playerStats = playerStatsManager.playerStats;
        lastAttackedTime = Time.time;

        if (playerStats.currentShield > 0)
        {
            float shieldDepletion = Mathf.Min(shieldDamage, playerStats.currentShield);
            playerStats.currentShield -= shieldDepletion;
            float excessShieldDamage = shieldDamage - shieldDepletion;
            playerStats.currentHP -= (hpDamage + excessShieldDamage);
        }
        else
        {
            playerStats.currentHP -= hpDamage;
        }

        if (playerStats.currentHP <= 0)
        {
            HandleDeath();
            playerStats.currentHP = 0;
        }

        if (syncPlayerStats != null)
        {
            syncPlayerStats.ServerSetCurrentHP(playerStats.currentHP);
            syncPlayerStats.ServerSetCurrentShield(playerStats.currentShield);
            if (NetworkServer.active && damageMagnitude > 0.0001f)
                syncPlayerStats.ServerRegisterCombatActivity();
        }
        if (playerHealthUi != null && syncPlayerStats == null)
        {
            playerHealthUi.UpdateShield(playerStats.currentShield);
            playerHealthUi.UpdateHealth(playerStats.currentHP);
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

            if (syncPlayerStats != null)
            {
                syncPlayerStats.ServerSetCurrentHP(playerStats.currentHP);
                syncPlayerStats.ServerSetCurrentShield(playerStats.currentShield);
            }
            if (playerHealthUi != null && syncPlayerStats == null)
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

    /// <summary>Combat snapshot: server uses authoritative <see cref="PlayerStatsManager"/>; client falls back to local stats for UI.</summary>
    public CombatStatsSnapshot GetCombatStatsSnapshot()
    {
        if (syncPlayerStats != null)
        {
            var snapshot = syncPlayerStats.GetCombatStatsSnapshot();
            if (snapshot.IsValid)
                return snapshot;
        }
        return playerStatsManager != null ? CombatStatsSnapshot.From(playerStatsManager.playerStats) : default;
    }
}
