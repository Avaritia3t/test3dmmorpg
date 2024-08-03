using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDCanvas : MonoBehaviour
{
    public static HUDCanvas Instance { get; private set; }

    public TMP_Text currentDamageValue;
    public TMP_Text inevitableDamageValue;
    public TMP_Text criticalChanceValue;
    public TMP_Text attackRangeValue;
    public TMP_Text criticalDamageValue;
    public TMP_Text maxHPValue;
    public TMP_Text moveSpeedValue;
    public TMP_Text maxShieldValue;
    public TMP_Text stunChanceValue;
    public TMP_Text physicalLifestealValue;

    private Dictionary<string, TMP_Text> statTextMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        statTextMap = new Dictionary<string, TMP_Text>
        {
            { "currentDamage", currentDamageValue },
            { "inevitableDamage", inevitableDamageValue },
            { "criticalChance", criticalChanceValue },
            { "attackRange", attackRangeValue },
            { "criticalDamage", criticalDamageValue },
            { "maxHP", maxHPValue }, // Will be used to show currentHP / maxHP
            { "moveSpeed", moveSpeedValue },
            { "maxShield", maxShieldValue }, // Will be used to show currentShield / maxShield
            { "stunChance", stunChanceValue },
            { "physicalLifesteal", physicalLifestealValue }
        };
    }

    private void Update()
    {
        if (PlayerStatsManager.Instance == null)
        {
            Debug.LogWarning("PlayerStatsManager.Instance is null.");
            return;
        }

        var playerStats = PlayerStatsManager.Instance.playerStats;
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStatsManager.Instance.playerStats is null.");
            return;
        }

        var statsDebugLog = new List<string>();

        foreach (var stat in statTextMap.Keys)
        {
            if (statTextMap[stat] == null)
            {
                Debug.LogWarning($"TMP_Text for {stat} is not assigned in the Inspector.");
                continue;
            }

            if (stat == "maxHP")
            {
                statTextMap[stat].text = $"{playerStats.currentHP.ToString("F0")} / {playerStats.maxHP.ToString("F0")}";
                statsDebugLog.Add($"maxHP: {playerStats.currentHP.ToString("F0")} / {playerStats.maxHP.ToString("F0")}");
            }
            else if (stat == "maxShield")
            {
                statTextMap[stat].text = $"{playerStats.currentShield.ToString("F0")} / {playerStats.maxShield.ToString("F0")}";
                statsDebugLog.Add($"maxShield: {playerStats.currentShield.ToString("F0")} / {playerStats.maxShield.ToString("F0")}");
            }
            else
            {
                var propertyInfo = playerStats.GetType().GetProperty(stat);
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(playerStats);
                    statTextMap[stat].text = ((float)value).ToString("F0");
                    statsDebugLog.Add($"{stat}: {((float)value).ToString("F0")}");
                }
                else
                {
                    // If not found as a property, check if it's a field
                    var fieldInfo = playerStats.GetType().GetField(stat);
                    if (fieldInfo != null)
                    {
                        var value = fieldInfo.GetValue(playerStats);
                        statTextMap[stat].text = ((float)value).ToString("F0");
                        statsDebugLog.Add($"{stat}: {((float)value).ToString("F0")}");
                    }
                    else
                    {
                        Debug.LogWarning($"Property or Field {stat} not found on PlayerStats.");
                    }
                }
            }
        }

        // Log all stats in a single line
        // Debug.Log($"HUD Stats Update: [{string.Join(", ", statsDebugLog)}]");
    }
}
