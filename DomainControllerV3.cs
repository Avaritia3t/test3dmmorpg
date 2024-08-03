using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class DomainControllerV3 : MonoBehaviour
{
    public static DomainControllerV3 Instance { get; private set; }

    private NavMeshAgent agent;
    public PlayerStatsManager playerStatsManager;
    public string factionName = "Heritage";
    public float moveSpeed = 100f;
    public float acceleration = 100f;
    private bool mapBuffsApplied = false;
    public twoplayerstatbarui playerhealthui;
    public Camera mainCamera;

    // Handlers
    private AttackHandlerV2 attackHandler;
    public Transform player;
    private Coroutine attackCoroutine;
    private Coroutine regenerateCoroutine;
    private GameObject selectedTarget;
    private float lastAttackedTime;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Debug.Log($"[Awake] Position: {transform.position}");
        agent = GetComponent<NavMeshAgent>();
        playerStatsManager = GetComponent<PlayerStatsManager>();
        playerhealthui = GameObject.Find("playerstatcanvas").GetComponent<twoplayerstatbarui>();

        agent.speed = moveSpeed;
        agent.acceleration = acceleration;

        InitializeAgent();
        ApplyMapBuffs();
    }

    private void Start()
    {
        InitializePlayerStatCanvas();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Only reassign if necessary
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found in the scene.");
                return;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            SelectTarget();
        }

        if (Input.GetMouseButton(0))
        {
            ClickToMove();
        }

        CheckForAttackCommand();

        if (isAttacking && selectedTarget != null)
        {
            AutoAttackInRange();
        }
    }

    private void CheckForAttackCommand()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            // Toggle the attack loop when CTRL is pressed
            isAttacking = !isAttacking;
            Debug.Log($"[CheckForAttackCommand] CTRL pressed. Attacking state is now: {isAttacking}");

            if (!isAttacking)
            {
                // If the attack was stopped, return the handler to the pool
                StopAttack();
            }
        }
    }

    private void SelectTarget()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask layerMask = ~LayerMask.GetMask("NavigationTerrain");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            var target = hit.collider.gameObject;
            if (target != null && target.layer == LayerMask.NameToLayer("SubdomainColliders"))
            {
                selectedTarget = target;
                // Debug.Log("Target selected: " + target.name);
            }
        }
    }

    private void InitializePlayerStatCanvas()
    {
        twoplayerstatbarui playerstatbar = GameObject.Find("playerstatcanvas").GetComponent<twoplayerstatbarui>();
        // Debug.Log("Attempting to access the twoplayerstatbarui script");
    }

    private void AutoAttackInRange()
    {
        if (selectedTarget == null || PlayerStatsManager.Instance.playerStats.currentHP <= 0)
        {
            Debug.Log("[AutoAttackInRange] No valid target or player is dead. Stopping attack.");
            isAttacking = false;
            return;
        }

        if (selectedTarget.GetComponent<SubdomainV2>()?.currentHP <= 0)
        {
            Debug.Log("[AutoAttackInRange] Target HP is zero. Stopping attack.");
            isAttacking = false;
            return;
        }

        if (CheckAttackInterval())
        {
            PerformAttack();
        }
    }

    private bool CheckAttackInterval()
    {
        float attackSpeed = PlayerStatsManager.Instance.playerStats.attackSpeed;
        float waitTime = 151.67f / attackSpeed - 0.0167f; // Attack speed formula

        if (Time.time - lastAttackTime >= waitTime)
        {
            Debug.Log("[CheckAttackInterval] Enough time has passed since the last attack. Ready to attack.");
            return true;
        }
        else
        {
            Debug.Log("[CheckAttackInterval] Not enough time has passed since the last attack.");
            return false;
        }
    }

    private void PerformAttack()
    {
        if (selectedTarget == null)
        {
            Debug.LogError("[PerformAttack] No target to attack. Exiting function.");
            return;
        }

        if (attackHandler == null)
        {
            Debug.Log("[PerformAttack] Requesting a new AttackHandler.");
            attackHandler = AttackHandlerPoolV2.Instance.RequestHandler();
            if (attackHandler == null)
            {
                Debug.LogError("[PerformAttack] Failed to get AttackHandler from the pool. Exiting function.");
                return;
            }
        }

        attackHandler.Initialize(this.gameObject, selectedTarget);

        Debug.Log("[PerformAttack] Attempting attack on target.");
        attackHandler.AttemptAttack(this.gameObject);

        lastAttackTime = Time.time; // Update the last attack time after a successful attack

        Debug.Log("[PerformAttack] Attack completed. Returning AttackHandler to the pool.");
        attackHandler.isAttacking = false;
        AttackHandlerPoolV2.Instance.ReturnHandler(attackHandler);
        attackHandler = null;
    }

    private void StopAttack()
    {
        if (attackHandler != null)
        {
            Debug.Log("[StopAttack] Returning AttackHandler to the pool.");
            attackHandler.isAttacking = false;
            AttackHandlerPoolV2.Instance.ReturnHandler(attackHandler);
            attackHandler = null;
        }
    }

    private void InitializeAgent()
    {
        var mapManager = MapManagerV3.Instance;
        if (mapManager == null || mapManager.currentMap == null || mapManager.currentMap.spawnPoint == null)
        {
            Debug.LogError("MapManager or currentMap or spawnPoint is null");
            return;
        }

        transform.position = mapManager.currentMap.spawnPoint;
    }

    private void ClickToMove()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int baseTerrainLayerMask = LayerMask.GetMask("BaseTerrain");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, baseTerrainLayerMask))
        {
            Vector3 navMeshPoint = new Vector3(hit.point.x, hit.point.y + 30, hit.point.z);

            if (NavMesh.SamplePosition(navMeshPoint, out NavMeshHit navHit, 10.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
            else
            {
                Debug.Log("Failed to find a valid point on the NavMesh near the clicked location.");
            }
        }
        else
        {
            // Debug.Log("Raycast did not hit the BaseTerrain layer.");
        }
    }

    public void TakeDamage(float hpDamage, float shieldDamage)
    {
        var playerStats = PlayerStatsManager.Instance.playerStats;
        lastAttackedTime = Time.time; // Update the last attacked time

        // Debug.Log($"DomainControllerV3 TakeDamage: Initial Shield = {playerStats.currentShield}, Initial HP = {playerStats.currentHP}");

        if (playerStats.currentShield > 0)
        {
            float shieldDepletion = Mathf.Min(shieldDamage, playerStats.currentShield);
            // Debug.Log($"DomainControllerV3 TakeDamage: Shield Damage Attempted = {shieldDamage}, Shield Depletion = {shieldDepletion}");

            playerStats.currentShield -= shieldDepletion;
            playerhealthui.UpdateShield(playerStats.currentShield);
            // Debug.Log($"DomainControllerV3 TakeDamage: Shield After Damage = {playerStats.currentShield}");

            float excessShieldDamage = shieldDamage - shieldDepletion;
            // Debug.Log($"DomainControllerV3 TakeDamage: Excess Shield Damage = {excessShieldDamage}, HP Damage = {hpDamage}");

            playerStats.currentHP -= (hpDamage + excessShieldDamage);
            playerhealthui.UpdateHealth(playerStats.currentHP);
            // Debug.Log($"DomainControllerV3 TakeDamage: HP After Taking Shield Excess Damage and HP Damage = {playerStats.currentHP}");
        }
        else
        {
            // Debug.Log($"DomainControllerV3 TakeDamage: No Shield, Taking HP Damage = {hpDamage}");
            playerStats.currentHP -= hpDamage;
            playerhealthui.UpdateHealth(playerStats.currentHP);
            // Debug.Log($"DomainControllerV3 TakeDamage: HP After Taking HP Damage = {playerStats.currentHP}");
        }

        if (playerStats.currentHP <= 0)
        {
            HandleDeath();
            Debug.Log("DomainControllerV3 TakeDamage: Player has died. Executing HandleDeath.");
            playerStats.currentHP = 0;
            playerhealthui.UpdateHealth(playerStats.currentHP);
            Debug.Log("DomainControllerV3 TakeDamage: HP set to 0 as player is dead.");
        }

        // Debug.Log($"Final Shield: {playerStats.currentShield}, Final HP: {playerStats.currentHP}");

        if (regenerateCoroutine != null)
        {
            StopCoroutine(regenerateCoroutine);
        }

        regenerateCoroutine = StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        var playerStats = PlayerStatsManager.Instance.playerStats;

        yield return new WaitForSeconds(playerStats.combatRegenDelay);

        while (playerStats.currentHP < playerStats.baseHP || playerStats.currentShield < playerStats.baseShield)
        {
            if (Time.time - lastAttackedTime < playerStats.combatRegenDelay)
            {
                regenerateCoroutine = null;
                yield break; // Exit the coroutine if attacked within the last 10 seconds
            }

            playerStats.currentHP += playerStats.baseHP * (playerStats.hpRegenPercent / 100f) * Time.deltaTime;
            playerStats.currentShield += playerStats.baseShield * (playerStats.shieldRegenPercent / 100f) * Time.deltaTime;

            playerStats.currentHP = Mathf.Min(playerStats.currentHP, playerStats.baseHP);
            playerStats.currentShield = Mathf.Min(playerStats.currentShield, playerStats.baseShield);

            if (playerhealthui != null)
            {
                playerhealthui.UpdateHealth(playerStats.currentHP);
                playerhealthui.UpdateShield(playerStats.currentShield);
            }

            yield return null;
        }

        regenerateCoroutine = null;
    }

    private void HandleDeath()
    {
        Debug.Log("Unit has died.");
    }

    public void ApplyMapBuffs()
    {
        if (!mapBuffsApplied)
        {
            MapManagerV3.Instance.ApplyMapBuffs(gameObject);
            mapBuffsApplied = true;
        }
    }
}
