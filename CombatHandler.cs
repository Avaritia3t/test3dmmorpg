using UnityEngine;

public class CombatHandler : MonoBehaviour
{
    public static CombatHandler Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Method to manage and resolve an attack from one unit to another
    public void ProcessAttack(GameObject attacker, GameObject target)
    {
        if (attacker == null || target == null)
            return;

        AttackHandlerV2 attackerHandler = attacker.GetComponent<AttackHandlerV2>();
        DomainControllerV2 targetController = target.GetComponent<DomainControllerV2>();

        if (attackerHandler != null && targetController != null)
        {
            // Pass attackerHandler's baseDamage to SetDamage
            var (hpDamage, shieldDamage) = attackerHandler.SetOutgoingDamage(attackerHandler.attackDamage);
            targetController.TakeDamage(hpDamage, shieldDamage);
        }
    }
}
