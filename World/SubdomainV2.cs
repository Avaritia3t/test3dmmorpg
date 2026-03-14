using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum SubdomainV2Type
{
    Badlands, Hovel, Hearthstead, Thorp, Borough, Civicron, Arcanopolis, Dominionhold, Sovereignty, Apex
}

[System.Serializable]
public class SubdomainV2 : MonoBehaviour
{
    public string subdomainName;
    public int level;
    public SubdomainV2Type type;
    public float depletionInterval;
    public float regenerationSpeed;
    public string factionTerritory;
    public string territoryModifier;

    public float SubdomainShield; // Base Shield strength or capacity
    public float SubdomainHP; // Base Health points
    public float SubdomainDamage; // Base Subdomain Damage
    public float SubdomainAttackSpeed; // Base Subdomain Attack Speed
    public float attackRange;
    public float currentHP;
    public float currentShield;
    public float deaggroTime = 10f;
    private npcstatbarui statsUI;
    public List<Buff> buffs; // List to hold any buffs applicable to the subdomain
    public bool isOwned; // Flag to check if the subdomain is owned
    public string ownerName; // Name of the owner if isOwned is true

    public List<NPC> npcs;
    public List<Resource> resources; // List of resources that the NPC can generate
    private List<Item> availableItems; // List of items that the NPC can generate
    public List<Item> spawnedItems;

    private float lastOccupiedTime;
    public float resourceGenerationInterval = 5f; // Interval in seconds

    public float spawnTime = 10f; // Time to start regenerating HP after defeat
    public float hpRegeneration = 1f; // HP regeneration rate (percentage per second)
    public float shieldRegeneration = 2f; // Shield regeneration rate (percentage per second)

    private AttackHandlerV2 attackHandler;
    private float lastAttackedTime;
    private float lastAttackTime;
    private GameObject lastAttacker;
    public Coroutine regenerateCoroutine;

    private bool isGeneratingItemsAndResources = false;

    public enum SubdomainState
    {
        Idle,
        UnderAttack,
        Retaliating,
        Aggravated,
        Defeated,
        Regenerating,
        Conquered,
        GeneratingResources
    }

    private SubdomainState currentState;
    public SubdomainState CurrentState => currentState;
    public bool IsGeneratingItemsAndResources => isGeneratingItemsAndResources;



    void Start()
    {
        subdomainName = gameObject.name;
        currentHP = SubdomainHP;
        currentShield = SubdomainShield;
        InitializeResourcesAndItems();
        currentState = SubdomainState.Idle;
    }

    void Update()
    {
        ExecuteSubdomainState(currentState);
    }

    private void ExecuteSubdomainState(SubdomainState state)
    {
        switch (state)
        {
            case SubdomainState.Idle:
                IdleBehavior();
                break;

            case SubdomainState.UnderAttack:
                DetermineAttackResponse(lastAttacker);
                break;

            case SubdomainState.Retaliating:
                Retaliate(lastAttacker);
                break;

            case SubdomainState.Aggravated:
                Aggravate();
                break;

            case SubdomainState.Defeated:
                HandleDeath();
                break;

            case SubdomainState.Regenerating:
                DelayedRegeneration();
                break;

            case SubdomainState.Conquered:
                // Logic for Conquered state
                break;

            case SubdomainState.GeneratingResources:
                // Logic for GeneratingResources state
                break;

            default:
                Debug.LogError("Unknown SubdomainState: " + state);
                break;
        }
    }

    public void SetStatsUI(npcstatbarui ui)
    {
        statsUI = ui;
        if (statsUI != null)
        {
            statsUI.SetMaxHealth(SubdomainHP);
            statsUI.SetMaxShield(SubdomainShield);
            statsUI.gameObject.SetActive(true); // Activate the UI once it's ready
        }
        else
        {
            Debug.LogError("Failed to set Stats UI - provided UI component is null.");
        }
    }

    public void CreateSubdomainStats()
    {
        level = Random.Range(1, 21); // Assign a random level between 1 and 20
        type = GenerateType(); // Set the type based on weighted random selection
        AdjustStatsByType(); // Adjust stats based on the generated type
        AdjustStatsByLevel(); // Adjust stats based on the generated level

        if (statsUI != null)
        {
            statsUI.SetMaxHealth(SubdomainHP);
            statsUI.SetMaxShield(SubdomainShield);
            statsUI.gameObject.SetActive(true); // Activate the UI once it's ready
        }
    }

    private void InitializeResourcesAndItems()
    {
        DropRule rule = DropRules.Instance.dropRules.Find(r => r.subdomainType == type);

        if (rule != null)
        {
            InitializeResources(rule);
            InitializeItems(rule); // Assuming `level` is the subdomain's level
        }
        else
        {
            Debug.LogError("No drop rule found for subdomain type: " + type);
        }
    }

    private void InitializeResources(DropRule rule)
    {
        resources = new List<Resource>();
        foreach (var resourceType in rule.allowedResources)
        {
            resources.Add(new Resource(resourceType, ResourceGrade.Standard, 0, UnityEngine.Random.Range(0.1f, 1f)));
        }
    }

    private void InitializeItems(DropRule rule)
    {
        availableItems = new List<Item>();
        spawnedItems = new List<Item>();

        foreach (var itemType in rule.allowedItemTypes)
        {
            Item newItem = DropRules.GenerateEmptyItem(itemType);

            if (newItem != null)
            {
                AssignItemStatsBySubdomain(newItem, rule.subdomainType); // Fetch level dynamically
                availableItems.Add(newItem);
                // Debug.Log($"Initialized new item: {newItem.itemName} for subdomain: {rule.subdomainType} at level: {level}");
            }
        }
    }

    private void AssignItemStatsBySubdomain(Item item, SubdomainV2Type subdomainType)
    {
        int maxStats = 0;
        List<string> statPool = new List<string>();

        switch (subdomainType)
        {
            case SubdomainV2Type.Arcanopolis:
                maxStats = 2;
                statPool.AddRange(ItemStats.OffensiveStats);
                break;
            case SubdomainV2Type.Dominionhold:
                maxStats = 3;
                statPool.AddRange(ItemStats.DefensiveStats);
                break;
            case SubdomainV2Type.Sovereignty:
                maxStats = 4;
                statPool.AddRange(ItemStats.OffensiveStats);
                statPool.AddRange(ItemStats.DefensiveStats);
                break;
            case SubdomainV2Type.Apex:
                maxStats = 5;
                statPool.AddRange(ItemStats.OffensiveStats);
                statPool.AddRange(ItemStats.DefensiveStats);
                statPool.AddRange(ItemStats.UtilityStats);
                break;
        }

        int numStats = Mathf.Min(UnityEngine.Random.Range(1, maxStats + 1), statPool.Count);

        for (int i = 0; i < numStats; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, statPool.Count);
            string statName = statPool[randomIndex];
            statPool.RemoveAt(randomIndex);

            float baseValue = UnityEngine.Random.Range(10f, 100f);
            float scaledValue = baseValue * (1 + (0.02f * level)); // Use dynamic level

            item.stats.Add(new ItemStat(statName, scaledValue));
        }
    }

    private void GenerateResources()
    {
        foreach (var resource in resources)
        {
            resource.quantity += Mathf.FloorToInt(resource.regenerationSpeed * regenerationSpeed);
        }
    }

    private void GenerateItems()
    {
        DropRule rule = DropRules.Instance.dropRules.Find(r => r.subdomainType == type);

        if (rule != null)
        {
            foreach (var itemType in rule.allowedItemTypes)
            {
                // Check if an item should be generated based on its generation rate
                if (UnityEngine.Random.Range(0f, 1f) <= 0.1f) // Adjust the generation rate as needed
                {
                    Item newItem = CreateRandomItem(itemType);

                    // Assign stats based on subdomain rules
                    AssignItemStatsBySubdomain(newItem, type);

                    // Add the generated item to the spawned items list
                    spawnedItems.Add(newItem);

                    // Print out the spawned item
                    // Debug.Log($"Generated item: {newItem.itemName}, Type: {newItem.itemType}, Subtype: {newItem.subtype}");
                }
            }
        }
        else
        {
            Debug.LogError("No drop rule found for subdomain type: " + type);
        }
    }

    private Item CreateRandomItem(ItemType itemType)
    {
        string subtype = GenerateRandomSubtype(itemType);
        string iconPath = $"EquipmentIcons/{subtype.ToLower()}";
        Sprite icon = Resources.Load<Sprite>(iconPath);

        if (icon == null)
        {
            Debug.LogError($"Icon not found at path: {iconPath}");
        }
        else
        {
            // Debug.Log($"Loaded icon for {subtype} from path: {iconPath}");
        }

        return new Item
        (
            itemName: subtype,
            itemType: itemType,
            subtype: subtype,
            itemRarity: (ItemRarity)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ItemRarity)).Length),
            flavorText: $"A {subtype} {itemType}",
            generationRate: 0.01f,
            damageType: (itemType == ItemType.Weapon || itemType == ItemType.Phalanx) ? GenerateRandomDamageType() : DamageType.None,
            damageMin: 0,
            damageMax: 0,
            stats: new List<ItemStat>(),
            level: level,
            icon: icon
        );
    }

    private string GenerateRandomSubtype(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                var weaponSubtypes = new List<string> { "AncientLaser", "OffLaser", "HiTechLaser", "RedLaser", "WhiteLaser" };
                return weaponSubtypes[UnityEngine.Random.Range(0, weaponSubtypes.Count)];
            case ItemType.Phalanx:
                var phalanxSubtypes = new List<string> { "BlueGenerator", "WhiteGenerator", "RedGenerator", "PurpleGenerator" };
                return phalanxSubtypes[UnityEngine.Random.Range(0, phalanxSubtypes.Count)];
            case ItemType.Artefact:
                var artefactSubtypes = new List<string> { "GoldenSkull", "Microcosm" };
                return artefactSubtypes[UnityEngine.Random.Range(0, artefactSubtypes.Count)];
            default:
                return "UnknownSubtype";
        }
    }

    private DamageType GenerateRandomDamageType()
    {
        float roll = UnityEngine.Random.value;
        if (roll < 0.40f)
            return DamageType.Physical;
        else if (roll < 0.80f)
            return DamageType.Ethereal;
        else if (roll < 0.96f)
            return DamageType.Demonic;
        else if (roll < 0.99f)
            return DamageType.Affliction;
        else
            return DamageType.Inevitable;
    }

    private IEnumerator GenerateItemsAndResources()
    {
        if (isGeneratingItemsAndResources)
        {
            Debug.Log("isGeneratingItemsAndResources already true somewhere. Breaking GenerateItemsAndResources Coroutine");
            yield break; // Prevent multiple starts
        }

        isGeneratingItemsAndResources = true;

        while (isGeneratingItemsAndResources)
        {
            GenerateResources();
            GenerateItems();

            yield return new WaitForSeconds(resourceGenerationInterval); // Wait for the next interval
        }

        isGeneratingItemsAndResources = false;
        // Debug.Log("No longer generating items and resources.");
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
            currentState = SubdomainState.Defeated;
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
            currentState = SubdomainState.Defeated;
        }
        else
        {
            currentState = SubdomainState.UnderAttack;
        }
    }

    private void ReleaseAttackHandler()
    {
        if (attackHandler != null)
        {
            attackHandler.isAttacking = false;
            AttackHandlerPoolV2.Instance.ReturnHandler(attackHandler);
            attackHandler = null;
            Debug.Log("[ReleaseAttackHandler] AttackHandler released and returned to pool.");
        }
    }

    private void DetermineAttackResponse(GameObject attacker)
    {
        // If the subdomain's HP is zero or less, set state to Defeated
        if (currentHP <= 0)
        {
            currentState = SubdomainState.Defeated;
            return;
        }

        // If the attacker is out of range, set state to Aggravated
        if (Vector3.Distance(transform.position, attacker.transform.position) > attackRange)
        {
            currentState = SubdomainState.Aggravated;
            return;
        }

        // If the attacker is in range, set state to Retaliating
        currentState = SubdomainState.Retaliating;
    }

    private void Aggravate()
    {
        if (attackHandler == null)
        {
            attackHandler = AttackHandlerPoolV2.Instance.RequestHandler();
            attackHandler.Initialize(this.gameObject, lastAttacker);
        }

        StartCoroutine(AggravateCoroutine());
    }

    private IEnumerator AggravateCoroutine()
    {
        float aggravationDuration = 10f; // Time to wait while aggravated
        float elapsed = 0f;

        while (elapsed < aggravationDuration)
        {
            // Check if the attacker comes into range during aggravation
            if (Vector3.Distance(transform.position, lastAttacker.transform.position) <= attackRange)
            {
                currentState = SubdomainState.Retaliating;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // After 10 seconds, reset the state (e.g., to Idle or another appropriate state)
        currentState = SubdomainState.Idle;
    }

    private void Retaliate(GameObject attacker)
    {
        // Ensure attackHandler is initialized
        if (attackHandler == null)
        {
            attackHandler = AttackHandlerPoolV2.Instance.RequestHandler();
            if (attackHandler == null)
            {
                Debug.LogError("[Retaliate] Failed to get AttackHandler from the pool. Exiting Retaliate.");
                return;
            }
            attackHandler.Initialize(this.gameObject, attacker); // Set the attacker and the target here
        }

        // If not enough time has passed since the last attack, do nothing
        float waitTime = 151.67f / attackHandler.attackSpeed - 0.0167f;
        if (Time.time - lastAttackTime < waitTime)
        {
            return; // Exit if the wait time hasn't passed
        }

        // Check if the attacker is still in range
        if (Vector3.Distance(transform.position, attackHandler.attackTarget.transform.position) > attackHandler.attackRange)
        {
            currentState = SubdomainState.Aggravated;
            ReleaseAttackHandler();
            return;
        }

        // If the target's HP is 0, do nothing
        DomainControllerV3 playerController = attackHandler.attackTarget.GetComponent<DomainControllerV3>();
        if (playerController != null && PlayerStatsManager.Instance.playerStats.currentHP <= 0)
        {
            attackHandler.isAttacking = false;
            ReleaseAttackHandler();
            return; // Exit if the player's HP is 0
        }

        // If the target is alive and in range, attack
        if (attackHandler == null)
        {
            attackHandler = AttackHandlerPoolV2.Instance.RequestHandler();
        }

        attackHandler.Initialize(this.gameObject, attackHandler.attackTarget);
        attackHandler.AttemptAttack(this.gameObject);

        lastAttackTime = Time.time; // Update last attack time after a successful attack

        ReleaseAttackHandler();
    }

    private void HandleDeath()
    {
        currentHP = 0;
        currentShield = 0;

        TransferResourcesAndItemsToPlayer();
        PlayerStatsManager.Instance.AddExperience(CalculateExpReward());

        currentState = SubdomainState.Regenerating;
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
        Debug.Log("[Regenerating] NPC defeated. Waiting 10 seconds before starting regeneration.");

        // Wait for 10 seconds
        yield return new WaitForSeconds(10f);

        // Start the regeneration process
        StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        ReleaseAttackHandler();
        isGeneratingItemsAndResources = true;
        currentState = SubdomainState.Regenerating;

        while (currentHP < SubdomainHP || currentShield < SubdomainShield)
        {
            currentHP += SubdomainHP * (hpRegeneration / 100f) * Time.deltaTime;
            currentShield += SubdomainShield * (shieldRegeneration / 100f) * Time.deltaTime;

            currentHP = Mathf.Min(currentHP, SubdomainHP);
            currentShield = Mathf.Min(currentShield, SubdomainShield);

            if (statsUI != null)
            {
                statsUI.UpdateHealth(currentHP);
                statsUI.UpdateShield(currentShield);
            }

            yield return null;
        }

        currentState = SubdomainState.Idle;
        regenerateCoroutine = null;
    }

    private void TransferResourcesAndItemsToPlayer()
    {
        foreach (var resource in resources)
        {
            InventoryManager.Instance.AddResource(resource);
            resource.quantity = 0; // Set quantity to zero after transfer
        }

        List<Item> itemsToRemove = new List<Item>();
        foreach (var item in spawnedItems)
        {
            InventoryManager.Instance.AddItem(item);
            itemsToRemove.Add(item); // Add item to the list of items to remove
        }

        foreach (var item in itemsToRemove)
        {
            spawnedItems.Remove(item);
        }

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