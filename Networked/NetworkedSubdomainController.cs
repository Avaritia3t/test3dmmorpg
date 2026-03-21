using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Server-authoritative subdomain: state machine, combat, loot. Uses SubdomainV2Type/SubdomainState from World/SubdomainV2.cs.
/// Required: on subdomain prefab (server-spawned), with NetworkIdentity, SyncSubdomainState. Optional: SubdomainItemGenerator, npcstatbarui for UI.
/// </summary>
[System.Serializable]
public class NetworkedSubdomainController : MonoBehaviour
{
    [Header("Identity & type (server-authoritative)")]
    public string subdomainName;
    public int level;
    public SubdomainV2Type type;

    [Header("Combat & resources")]
    public float SubdomainShield;
    public float SubdomainHP;
    public float SubdomainDamage;
    public float SubdomainAttackSpeed;
    public float attackRange;
    public float currentHP;
    public float currentShield;
    [SerializeField] private float deaggroTime = 10f;
    [SerializeField] private float resourceGenerationInterval = 5f;
    [SerializeField] private float spawnTime = 10f;
    [SerializeField] private float hpRegeneration = 1f;
    [SerializeField] private float shieldRegeneration = 2f;

    public float regenerationSpeed;
    public string factionTerritory;
    public string territoryModifier;
    public List<Buff> buffs;
    public bool isOwned;
    public string ownerName;
    public List<NPC> npcs;
    public List<Resource> resources;
    public List<Item> spawnedItems;

    private float depletionInterval;
    private npcstatbarui statsUI;
    private List<Item> availableItems;
    private float lastOccupiedTime;
    private NetworkedAttackHandlerController attackHandler;
    private float lastAttackedTime;
    private float lastAttackTime;
    private GameObject lastAttacker;
    private Coroutine regenerateCoroutine;
    private Coroutine aggravateCoroutine;
    private bool isGeneratingItemsAndResources;
    private bool deathHandled;

    private IInventoryService inventoryService;
    private IDropRulesService dropRulesService;
    private INetworkedAttackHandlerPool networkedAttackHandlerPool;
    private IPlayerStatsService playerStatsService;
    private INetworkedLootService networkedLootService;
    private IInventoryService InventoryService => inventoryService ??= GameBootstrap.Locator?.Get<IInventoryService>();
    private IDropRulesService DropRulesService => dropRulesService ??= GameBootstrap.Locator?.Get<IDropRulesService>();
    private INetworkedAttackHandlerPool AttackHandlerPool => networkedAttackHandlerPool ??= GameBootstrap.Locator?.Get<INetworkedAttackHandlerPool>();
    private IPlayerStatsService PlayerStatsService => playerStatsService ??= GameBootstrap.Locator?.Get<IPlayerStatsService>();
    private INetworkedLootService NetworkedLootService => networkedLootService ??= GameBootstrap.Locator?.Get<INetworkedLootService>();

    private SubdomainItemGenerator itemGenerator;
    private Dictionary<SubdomainV2.SubdomainState, Action<GameObject>> stateHandlers;
    private SyncSubdomainState syncSubdomainState;

    private SubdomainV2.SubdomainState currentState;
    public SubdomainV2.SubdomainState CurrentState => currentState;
    public bool IsGeneratingItemsAndResources => isGeneratingItemsAndResources;

    private void Start()
    {
        syncSubdomainState = GetComponent<SyncSubdomainState>();
        subdomainName = gameObject.name;
        currentHP = SubdomainHP;
        currentShield = SubdomainShield;
        itemGenerator = GetComponent<SubdomainItemGenerator>();
        BuildStateHandlers();
        InitializeResourcesAndItems();
        currentState = SubdomainV2.SubdomainState.Idle;
        if (syncSubdomainState != null && NetworkServer.active)
            PushStateToSync();
    }

    private void BuildStateHandlers()
    {
        stateHandlers = new Dictionary<SubdomainV2.SubdomainState, Action<GameObject>>
        {
            { SubdomainV2.SubdomainState.Idle, _ => IdleBehavior() },
            { SubdomainV2.SubdomainState.UnderAttack, DetermineAttackResponse },
            { SubdomainV2.SubdomainState.Retaliating, Retaliate },
            { SubdomainV2.SubdomainState.Aggravated, _ => Aggravate() },
            { SubdomainV2.SubdomainState.Defeated, _ => HandleDeath() },
            { SubdomainV2.SubdomainState.Regenerating, _ => DelayedRegeneration() },
            { SubdomainV2.SubdomainState.Conquered, _ => { } },
            { SubdomainV2.SubdomainState.GeneratingResources, _ => { } }
        };
    }

    private void Update()
    {
        if (syncSubdomainState != null && !NetworkServer.active)
        {
            currentHP = syncSubdomainState.currentHP;
            currentShield = syncSubdomainState.currentShield;
            currentState = syncSubdomainState.CurrentState;
            if (statsUI != null)
            {
                statsUI.UpdateHealth(currentHP);
                statsUI.UpdateShield(currentShield);
            }
            return;
        }
        if (!NetworkServer.active)
            return;
        if (stateHandlers != null && stateHandlers.TryGetValue(currentState, out Action<GameObject> handler))
            handler?.Invoke(lastAttacker);
        else
            Debug.LogError("[NetworkedSubdomainController] Unknown SubdomainState: " + currentState);
        PushStateToSync();
    }

    private void PushStateToSync()
    {
        if (syncSubdomainState == null || !NetworkServer.active) return;
        syncSubdomainState.ServerSetHP(currentHP);
        syncSubdomainState.ServerSetShield(currentShield);
        syncSubdomainState.ServerSetState(currentState);
    }

    public void SetStatsUI(npcstatbarui ui)
    {
        statsUI = ui;
        if (statsUI == null)
        {
            Debug.LogError("[NetworkedSubdomainController] SetStatsUI: provided UI component is null.");
            return;
        }
        statsUI.SetMaxHealth(SubdomainHP);
        statsUI.SetMaxShield(SubdomainShield);
        statsUI.gameObject.SetActive(true);
    }

    /// <summary>Local roll for level/type and apply. Use in single-player or fallback; for networked use ApplySubdomainStatsFromServer.</summary>
    public void RollAndApplySubdomainStats()
    {
        level = UnityEngine.Random.Range(1, 21);
        type = GenerateType();
        ApplySubdomainStatsFromServer(level, type);
    }

    /// <summary>Server-authoritative: set level and type then recalc stats. Call from server sync so all clients see same subdomain.</summary>
    public void ApplySubdomainStatsFromServer(int serverLevel, SubdomainV2Type serverType)
    {
        level = serverLevel;
        type = serverType;
        AdjustStatsByType();
        AdjustStatsByLevel();
        if (statsUI != null)
        {
            statsUI.SetMaxHealth(SubdomainHP);
            statsUI.SetMaxShield(SubdomainShield);
            statsUI.gameObject.SetActive(true);
        }
    }

    /// <summary>Backward-compat name; calls RollAndApplySubdomainStats.</summary>
    public void CreateSubdomainStats() => RollAndApplySubdomainStats();

    private void InitializeResourcesAndItems()
    {
        if (DropRulesService == null)
        {
            Debug.LogError("[NetworkedSubdomainController] IDropRulesService not found. Ensure GameBootstrap runs and DropRules is in the scene.");
            return;
        }
        DropRule rule = DropRulesService.dropRules.Find(r => r.subdomainType == type);
        if (rule == null)
        {
            Debug.LogError("[NetworkedSubdomainController] No drop rule found for subdomain type: " + type);
            return;
        }
        InitializeResources(rule);
        if (itemGenerator != null)
        {
            availableItems = itemGenerator.InitializeItems(rule, DropRulesService, level);
            spawnedItems = new List<Item>();
        }
        else
        {
            availableItems = new List<Item>();
            spawnedItems = new List<Item>();
            Debug.LogWarning("[NetworkedSubdomainController] SubdomainItemGenerator not found. Item generation disabled.");
        }
    }

    private void InitializeResources(DropRule rule)
    {
        resources = new List<Resource>();
        foreach (var resourceType in rule.allowedResources)
            resources.Add(new Resource(resourceType, ResourceGrade.Standard, 0, UnityEngine.Random.Range(0.1f, 1f)));
    }

    private void GenerateResources()
    {
        if (resources == null) return;
        foreach (var resource in resources)
        {
            resource.quantity += Mathf.FloorToInt(resource.regenerationSpeed * regenerationSpeed);
        }
    }

    private void GenerateItems()
    {
        if (DropRulesService == null || itemGenerator == null) return;
        DropRule rule = DropRulesService.dropRules.Find(r => r.subdomainType == type);
        if (rule == null)
        {
            Debug.LogError("[NetworkedSubdomainController] No drop rule found for subdomain type: " + type);
            return;
        }
        foreach (var itemType in rule.allowedItemTypes)
        {
            if (UnityEngine.Random.Range(0f, 1f) <= 0.1f) // ten percent chance to generate items.
            {
                Item newItem = itemGenerator.GenerateItemForSubdomain(itemType, type, level);
                if (newItem != null)
                    spawnedItems.Add(newItem);
            }
        }
    }

    private IEnumerator GenerateItemsAndResources()
    {
        if (isGeneratingItemsAndResources)
        {
            // Debug.Log("[NetworkedSubdomainController] GenerateItemsAndResources already running.");
            yield break;
        }
        isGeneratingItemsAndResources = true;
        while (isGeneratingItemsAndResources)
        {
            GenerateResources();
            GenerateItems();
            yield return new WaitForSeconds(resourceGenerationInterval);
        }
        isGeneratingItemsAndResources = false;
    }

    private void IdleBehavior()
    {
        ReleaseAttackHandler();

        // Check if health or shield is not full
        if (currentHP < SubdomainHP || currentShield < SubdomainShield)
        {
            // Start the regeneration coroutine if it's not already running
            if (regenerateCoroutine == null)
            {
                regenerateCoroutine = StartCoroutine(Regenerate());
            }
        }

        // Start generating items and resources if it's not already doing so
        if (!isGeneratingItemsAndResources)
        {
            StartCoroutine(GenerateItemsAndResources());
        }
    }

    public void TakeDamage(float hpDamage, float shieldDamage, GameObject attacker)
    {
        lastAttacker = attacker;  // Store the attacker for later use in Retaliate
        lastAttackedTime = Time.time;
        isGeneratingItemsAndResources = false;

        // If the subdomain is already defeated, exit the function immediately
        if (currentHP <= 0)
        {
            currentHP = 0;
            currentState = SubdomainV2.SubdomainState.Defeated;
            return;
        }

        if (currentShield > 0)
        {
            float shieldDepletion = Mathf.Min(shieldDamage, currentShield);
            currentShield -= shieldDepletion;
            float excessShieldDamage = shieldDamage - shieldDepletion;
            currentHP -= (hpDamage + excessShieldDamage);
        }
        else
        {
            currentHP -= hpDamage + shieldDamage;
        }

        // Ensure current HP doesn't drop below zero
        currentHP = Mathf.Max(currentHP, 0);

        if (statsUI != null)
        {
            statsUI.UpdateHealth(currentHP);
            statsUI.UpdateShield(currentShield);
        }

        // If HP drops to zero or below after damage, set state to Defeated
        if (currentHP <= 0)
        {
            currentHP = 0;
            currentState = SubdomainV2.SubdomainState.Defeated;
        }
        else
        {
            currentState = SubdomainV2.SubdomainState.UnderAttack;
        }
        PushStateToSync();
    }

    private void ReleaseAttackHandler()
    {
        if (attackHandler == null) return;
        if (AttackHandlerPool != null)
        {
            attackHandler.isAttacking = false;
            AttackHandlerPool.ReturnHandler(attackHandler);
        }
        attackHandler = null;
        // Debug.Log("[NetworkedSubdomainController] AttackHandler released and returned to pool.");
    }

    private void DetermineAttackResponse(GameObject attacker)
    {
        if (attacker == null || (Time.time - lastAttackedTime > deaggroTime))
        {
            currentState = SubdomainV2.SubdomainState.Idle;
            ReleaseAttackHandler();
            return;
        }
        if (currentHP <= 0)
        {
            currentState = SubdomainV2.SubdomainState.Defeated;
            return;
        }
        if (Vector3.Distance(transform.position, attacker.transform.position) > attackRange)
        {
            currentState = SubdomainV2.SubdomainState.Aggravated;
            return;
        }
        currentState = SubdomainV2.SubdomainState.Retaliating;
    }

    private void Aggravate()
    {
        if (lastAttacker == null)
        {
            currentState = SubdomainV2.SubdomainState.Idle;
            ReleaseAttackHandler();
            return;
        }
        if (aggravateCoroutine != null)
            return;
        if (attackHandler == null && AttackHandlerPool != null)
        {
            attackHandler = AttackHandlerPool.RequestHandler();
            if (attackHandler != null)
                attackHandler.Initialize(this.gameObject, lastAttacker);
        }
        aggravateCoroutine = StartCoroutine(AggravateCoroutine());
    }

    private IEnumerator AggravateCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < deaggroTime)
        {
            if (lastAttacker == null)
            {
                currentState = SubdomainV2.SubdomainState.Idle;
                ReleaseAttackHandler();
                aggravateCoroutine = null;
                yield break;
            }
            if (Vector3.Distance(transform.position, lastAttacker.transform.position) <= attackRange)
            {
                currentState = SubdomainV2.SubdomainState.Retaliating;
                aggravateCoroutine = null;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentState = SubdomainV2.SubdomainState.Idle;
        aggravateCoroutine = null;
    }

    private void Retaliate(GameObject attacker)
    {
        if (AttackHandlerPool == null) return;
        if (attacker == null)
        {
            currentState = SubdomainV2.SubdomainState.Idle;
            ReleaseAttackHandler();
            return;
        }

        if (attackHandler == null)
        {
            attackHandler = AttackHandlerPool.RequestHandler();
            if (attackHandler == null)
            {
                Debug.LogError("[NetworkedSubdomainController] Retaliate: failed to get handler from pool.");
                return;
            }
            attackHandler.Initialize(this.gameObject, attacker);
        }

        float waitTime = 151.67f / attackHandler.attackSpeed - 0.0167f;
        if (Time.time - lastAttackTime < waitTime)
            return;

        if (attackHandler.attackTarget == null)
        {
            currentState = SubdomainV2.SubdomainState.Idle;
            ReleaseAttackHandler();
            return;
        }
        if (Vector3.Distance(transform.position, attackHandler.attackTarget.transform.position) > attackHandler.attackRange)
        {
            currentState = SubdomainV2.SubdomainState.Aggravated;
            ReleaseAttackHandler();
            return;
        }

        var targetController = attackHandler.attackTarget.GetComponent<NetworkedDomainController>();
        float targetHp = targetController != null ? targetController.GetCurrentHP() : 0f;
        if (targetHp <= 0f)
        {
            attackHandler.isAttacking = false;
            ReleaseAttackHandler();
            return;
        }

        attackHandler.Initialize(this.gameObject, attackHandler.attackTarget);
        attackHandler.AttemptAttack(this.gameObject);
        lastAttackTime = Time.time;
    }

    private void HandleDeath()
    {
        if (deathHandled)
            return;
        deathHandled = true;

        currentHP = 0;
        currentShield = 0;

        TransferResourcesAndItemsToPlayer();

        currentState = SubdomainV2.SubdomainState.Regenerating;
        ReleaseAttackHandler();
    }

    private void DelayedRegeneration()
    {
        if (regenerateCoroutine == null)
        {
            regenerateCoroutine = StartCoroutine(RegenerationCoroutine());
        }
    }

    private IEnumerator RegenerationCoroutine()
    {
        // Debug.Log("[NetworkedSubdomainController] Defeated. Waiting before regeneration.");
        yield return new WaitForSeconds(spawnTime);

        regenerateCoroutine = StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        ReleaseAttackHandler();
        isGeneratingItemsAndResources = true;
        currentState = SubdomainV2.SubdomainState.Regenerating;

        while (currentHP < SubdomainHP || currentShield < SubdomainShield)
        {
            currentHP += SubdomainHP * (hpRegeneration / 100f) * Time.deltaTime;
            currentShield += SubdomainShield * (shieldRegeneration / 100f) * Time.deltaTime;

            currentHP = Mathf.Min(currentHP, SubdomainHP);
            currentShield = Mathf.Min(currentShield, SubdomainShield);

            PushStateToSync();

            if (statsUI != null)
            {
                statsUI.UpdateHealth(currentHP);
                statsUI.UpdateShield(currentShield);
            }

            yield return null;
        }

        currentState = SubdomainV2.SubdomainState.Idle;
        regenerateCoroutine = null;
        deathHandled = false;
    }

    private void TransferResourcesAndItemsToPlayer()
    {
        if (!NetworkServer.active) return;
        if (resources == null) return;

        var conn = lastAttacker != null ? lastAttacker.GetComponent<NetworkIdentity>()?.connectionToClient : null;

        if (NetworkedLootService != null && conn != null)
        {
            foreach (var resource in resources)
            {
                NetworkedLootService.AddResourceForConnection(conn, resource);
                resource.quantity = 0;
            }
            if (spawnedItems != null)
            {
                foreach (var item in spawnedItems)
                    NetworkedLootService.AddItemForConnection(conn, item);
                spawnedItems.Clear();
            }
            NetworkedLootService.AddExperienceForConnection(conn, CalculateExpReward());
            return;
        }

        // No loot service singleton: still route to the attacking player's receiver (per-connection inventory).
        if (conn != null)
        {
            var recv = conn.identity != null ? conn.identity.GetComponent<IReceiveLoot>() : null;
            if (recv != null)
            {
                foreach (var resource in resources)
                {
                    recv.AddResource(resource);
                    resource.quantity = 0;
                }
                if (spawnedItems != null)
                {
                    foreach (var item in spawnedItems)
                        recv.AddItem(item);
                    spawnedItems.Clear();
                }
                recv.AddExperience(CalculateExpReward());
                return;
            }
        }

        if (InventoryService == null) return;
        foreach (var resource in resources)
        {
            InventoryService.AddResource(resource);
            resource.quantity = 0;
        }
        if (PlayerStatsService != null)
            PlayerStatsService.AddExperience(CalculateExpReward());

        if (spawnedItems == null) return;
        List<Item> itemsToRemove = new List<Item>();
        foreach (var item in spawnedItems)
        {
            InventoryService.AddItem(item);
            itemsToRemove.Add(item);
        }
        foreach (var item in itemsToRemove)
            spawnedItems.Remove(item);

        // Debug.Log("Logging Player inventory post-transfer.");
        // InventoryManager.Instance.LogPlayerInventory();

        // Optionally log the inventory state
        // LogInventory(); // Log the state of the inventory after transfer
    }

    private int CalculateExpReward()
    {
        // Simple example based on subdomain's level
        return level * 10;
    }

    private SubdomainV2Type GenerateType()
    {
        float randomValue = Random.value * 100; // Generate a random number between 0 and 100
        if (randomValue < 30) return SubdomainV2Type.Badlands;
        if (randomValue < 40) return SubdomainV2Type.Hovel;
        if (randomValue < 50) return SubdomainV2Type.Hearthstead;
        if (randomValue < 60) return SubdomainV2Type.Thorp;
        if (randomValue < 70) return SubdomainV2Type.Borough;
        if (randomValue < 80) return SubdomainV2Type.Civicron;
        if (randomValue < 85) return SubdomainV2Type.Arcanopolis;
        if (randomValue < 90) return SubdomainV2Type.Dominionhold;
        if (randomValue < 95) return SubdomainV2Type.Sovereignty;
        return SubdomainV2Type.Apex; // Anything above 95 falls into the Apex category
    }

    public void AdjustStatsByType()
    {
        int typeIndex = (int)type;
        SubdomainHP = 1000 + (typeIndex * 1000);
        SubdomainShield = 300 + (typeIndex * 60);
        SubdomainDamage = 10 + (typeIndex * 5);
        // Use the same range for NPCs as the player, adjust as needed
        SubdomainAttackSpeed = 100 + (typeIndex * 66.67f); // Example mapping
        regenerationSpeed = 10.0f + (typeIndex * 0.05f);
        attackRange = 80 + (typeIndex * 5);
        
    }

    public void AdjustStatsByLevel()
    {
        float levelMultiplier = 1 + (level * 0.02f); // Each level increases stats by 2%
        SubdomainHP *= levelMultiplier;
        SubdomainShield *= levelMultiplier;
        SubdomainDamage *= levelMultiplier;
        SubdomainAttackSpeed *= levelMultiplier;
        regenerationSpeed *= levelMultiplier;
        attackRange *= levelMultiplier;
    }

    public void UpdateSubdomainStatus()
    {
        if (resources == null) return;
        depletionInterval = Time.time - lastOccupiedTime;
        foreach (var resource in resources)
        {
            resource.quantity += Mathf.FloorToInt(depletionInterval * regenerationSpeed * resource.regenerationSpeed);
        }
        lastOccupiedTime = Time.time;
        depletionInterval = 0f;
    }

    public void OccupySubdomain()
    {
        lastOccupiedTime = Time.time;
        isOwned = true;
        ownerName = "GetOwnerName()"; // This will be replaced by an actual method to retrieve owner name
    }
}
