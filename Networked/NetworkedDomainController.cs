using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// Player controller for networked games: movement (local input), map buffs, combat via NetworkedPlayerCombatHelperController.
/// Required: on player prefab, with NetworkIdentity, SyncPlayerStats, NetworkedPlayerCombatHelperController, NavMeshAgent, PlayerStatsManager.
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
    private float lastCombatStatsSendTime;
    private const float CombatStatsSendInterval = 0.1f;

    // Combat snapshot sending optimization
    private bool combatStatsSendPending;
    private bool hasLastSentCombatStats;
    private CombatStatsSnapshot lastSentCombatStats;

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

        // Subscribe to client-side equipment changes so we only send snapshots when stats actually change.
        if (playerStatsManager != null)
            playerStatsManager.StatsChanged += OnPlayerStatsChanged;

        RequestCombatStatsResyncIfLocal();
    }

    private void OnDestroy()
    {
        if (playerStatsManager != null)
            playerStatsManager.StatsChanged -= OnPlayerStatsChanged;
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

        // Send snapshots only when there's a pending change, not every frame.
        if (combatStatsSendPending && Time.time - lastCombatStatsSendTime >= CombatStatsSendInterval)
            TrySendCombatStatsToServer();
    }

    private void OnPlayerStatsChanged()
    {
        combatStatsSendPending = true;
        if (networkIdentity != null && networkIdentity.isLocalPlayer)
        {
            // Attempt immediate send to reduce staleness after equip/unequip.
            if (Time.time - lastCombatStatsSendTime >= CombatStatsSendInterval)
                TrySendCombatStatsToServer();
        }
    }

    private void RequestCombatStatsResyncIfLocal()
    {
        if (networkIdentity != null && networkIdentity.isLocalPlayer)
            combatStatsSendPending = true;
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

    /// <summary>
    /// Local player only: push combat stats to server when they change.
    /// Uses last-sent comparison to avoid resending identical snapshots.
    /// </summary>
    private void TrySendCombatStatsToServer()
    {
        if (networkIdentity == null || !networkIdentity.isLocalPlayer || syncPlayerStats == null || playerStatsManager == null)
            return;

        var newSnapshot = CombatStatsSnapshot.From(playerStatsManager.playerStats);
        if (!newSnapshot.IsValid)
            return;

        if (hasLastSentCombatStats && !IsDifferent(newSnapshot, lastSentCombatStats))
        {
            combatStatsSendPending = false;
            return;
        }

        lastCombatStatsSendTime = Time.time;
        lastSentCombatStats = newSnapshot;
        hasLastSentCombatStats = true;
        syncPlayerStats.SendCombatStats(newSnapshot);
        combatStatsSendPending = false;
    }

    private static bool IsDifferent(CombatStatsSnapshot a, CombatStatsSnapshot b)
    {
        const float eps = 0.0001f;
        return Mathf.Abs(a.attackSpeed - b.attackSpeed) > eps ||
               Mathf.Abs(a.attackDamage - b.attackDamage) > eps ||
               Mathf.Abs(a.attackRange - b.attackRange) > eps ||
               Mathf.Abs(a.penetration - b.penetration) > eps ||
               Mathf.Abs(a.integrity - b.integrity) > eps ||
               Mathf.Abs(a.damageReduction - b.damageReduction) > eps ||
               Mathf.Abs(a.statusResistance - b.statusResistance) > eps ||
               Mathf.Abs(a.criticalChance - b.criticalChance) > eps ||
               Mathf.Abs(a.criticalDamage - b.criticalDamage) > eps ||
               Mathf.Abs(a.afflictionChance - b.afflictionChance) > eps ||
               Mathf.Abs(a.afflictionDamage - b.afflictionDamage) > eps ||
               Mathf.Abs(a.etherealChance - b.etherealChance) > eps ||
               Mathf.Abs(a.etherealDamage - b.etherealDamage) > eps ||
               Mathf.Abs(a.demonicChance - b.demonicChance) > eps ||
               Mathf.Abs(a.demonicDamage - b.demonicDamage) > eps ||
               Mathf.Abs(a.inevitableChance - b.inevitableChance) > eps ||
               Mathf.Abs(a.inevitableDamage - b.inevitableDamage) > eps ||
               Mathf.Abs(a.moveSpeed - b.moveSpeed) > eps;
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
            // else
            //     Debug.Log("[NetworkedDomainController] Failed to find a valid point on the NavMesh near the clicked location.");
        }
        // Debug.Log("Raycast did not hit the BaseTerrain layer.");
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

            // Map buffs modify stats directly (reflection). Ensure combat snapshot is resynced on the local client.
            RequestCombatStatsResyncIfLocal();
        }
    }

    /// <summary>Server/attack handler: get combat stats for this player. Prefer client-sent snapshot so equipment and buffs are correct.</summary>
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
