using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DomainControllerV2 : MonoBehaviour
{
    private NavMeshAgent agent;
    public string factionName = "Heritage";
    public float moveSpeed = 100f;
    public float acceleration = 100f;
    public float baseHP = 1000f;
    public float attackSpeed = 100f;
    public float currentDamage = 500f;
    public float attackRange = 200f;
    public float currentHP;
    public float currentShield;
    public float baseShield = 300f;
    private bool mapBuffsApplied = false;
    public twoplayerstatbarui playerhealthui;
    public Camera mainCamera;

    // Handlers
    private AttackHandlerV2 attackHandler;
    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.25f;
    public Transform player;
    private Coroutine attackCoroutine;

    void Awake()
    {
        Debug.Log($"[Awake] Position: {transform.position}");
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        InitializeAgent();
        attackHandler = GetComponent<AttackHandlerV2>();
        ApplyMapBuffs();
        playerhealthui = GameObject.Find("playerstatcanvas").GetComponent<twoplayerstatbarui>();
    }

    void Start()
    {
        currentHP = baseHP;
        currentShield = baseShield;
        ApplyMapBuffs();
        InitializePlayerStatCanvas();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            ClickToMove();
        }

        if (Input.GetMouseButtonDown(0) && (Time.time - lastClickTime < doubleClickThreshold))
        {
            AutoAttackInRange();
        }

        lastClickTime = Time.time;
    }

    private void InitializePlayerStatCanvas()
    {
        twoplayerstatbarui playerstatbar = GameObject.Find("playerstatcanvas").GetComponent<twoplayerstatbarui>();
        Debug.Log("Attempting to access the twoplayerstatbarui script");
    }

    private void AutoAttackInRange()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask layerMask = ~LayerMask.GetMask("NavigationTerrain");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            var target = hit.collider.gameObject;
            if (target != null && target.layer == LayerMask.NameToLayer("SubdomainColliders"))
            {
                attackHandler.SetTarget(target);

                if (attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                }

                attackCoroutine = StartCoroutine(ContinuousAttack());
            }
            else
            {
                Debug.Log("Target is null or not on the SubdomainColliders layer");
            }
        }
    }

    private IEnumerator ContinuousAttack()
    {
        var targetSubdomain = attackHandler.attackTarget.GetComponent<SubdomainV2>();
        float attackRange = attackHandler.attackRange;

        while (attackHandler.isAttacking && attackHandler.attackTarget != null && targetSubdomain.currentHP > 0)
        {
            float distance = Vector3.Distance(transform.position, attackHandler.attackTarget.transform.position);
            if (distance > attackRange)
            {
                attackHandler.isAttacking = false;
                break;
            }

            attackHandler.AttemptAttack(this.gameObject);

            float waitTime = 100f / attackSpeed;
            yield return new WaitForSeconds(waitTime);
        }

        attackHandler.isAttacking = false;
    }

    private void InitializeAgent()
    {
        var mapManager = MapManagerV2.Instance;
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
            Debug.Log("Raycast did not hit the BaseTerrain layer.");
        }
    }

    public void TakeDamage(float hpDamage, float shieldDamage)
    {
        Debug.Log($"Initial Shield: {currentShield}, Initial HP: {currentHP}");

        if (currentShield > 0)
        {
            float shieldDepletion = Mathf.Min(shieldDamage, currentShield);
            Debug.Log($"Shield Damage Attempted: {shieldDamage}, Shield Depletion: {shieldDepletion}");

            currentShield -= shieldDepletion;
            playerhealthui.UpdateShield(currentShield);
            Debug.Log($"Shield After Damage: {currentShield}");

            float excessShieldDamage = shieldDamage - shieldDepletion;
            Debug.Log($"Excess Shield Damage: {excessShieldDamage}, HP Damage: {hpDamage}");

            currentHP -= (hpDamage + excessShieldDamage);
            playerhealthui.UpdateHealth(currentHP);
            Debug.Log($"HP After Taking Shield Excess Damage and HP Damage: {currentHP}");
        }
        else
        {
            Debug.Log($"No Shield: Taking HP Damage: {hpDamage}");
            currentHP -= hpDamage;
            playerhealthui.UpdateHealth(currentHP);
            Debug.Log($"HP After Taking HP Damage: {currentHP}");
        }

        if (currentHP <= 0)
        {
            HandleDeath();
            Debug.Log("Player has died. Executing HandleDeath().");
            currentHP = 0;
            playerhealthui.UpdateHealth(currentHP);
            Debug.Log("HP set to 0 as player is dead.");
        }

        Debug.Log($"Final Shield: {currentShield}, Final HP: {currentHP}");
    }

    private void HandleDeath()
    {
        Debug.Log("Unit has died.");
    }

    public void ApplyMapBuffs()
    {
        if (!mapBuffsApplied)
        {
            MapManagerV2.Instance.ApplyMapBuffs(gameObject);
            mapBuffsApplied = true;
        }
    }
}
