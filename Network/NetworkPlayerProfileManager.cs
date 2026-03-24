using UnityEngine;

/// <summary>
/// Local profile bridge: reads/writes <see cref="PlayerStats"/> via <see cref="DBConnectionManager"/>.
/// <para><b>Mirror:</b> This is client-side persistence only. Server-authoritative stats should sync via
/// <see cref="Mirror.NetworkServer"/> / your networked components; use this for account selection before Host/Client.</para>
/// </summary>
public class NetworkPlayerProfileManager : MonoBehaviour
{
    public static NetworkPlayerProfileManager Instance { get; private set; }

    public string playerName;
    public string selectedFaction;
    public string selectedClass;

    private PlayerStats _playerStats;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _playerStats = new PlayerStats();
    }

    public void SetPlayerName(string name)
    {
        if (!string.IsNullOrEmpty(name))
            playerName = name;
        else
            Debug.LogError("[NetworkPlayerProfileManager] Cannot set empty player name.");
    }

    public void SetPlayerFaction(string forPlayerName, string factionName)
    {
        if (string.IsNullOrEmpty(forPlayerName))
        {
            Debug.LogError("[NetworkPlayerProfileManager] Player name not set. Cannot set faction.");
            return;
        }

        PlayerStats loaded = LoadStatsFromDatabase(forPlayerName);
        if (loaded == null)
        {
            Debug.LogError("[NetworkPlayerProfileManager] Failed to load stats for SetPlayerFaction.");
            return;
        }

        loaded.faction = factionName;
        loaded.hasSelectedFaction = true;
        SaveStatsToDatabase(loaded);
        Debug.Log($"[NetworkPlayerProfileManager] Faction set to {factionName} for {forPlayerName}");
    }

    public void SetPlayerClass(string forPlayerName, string className)
    {
        if (string.IsNullOrEmpty(forPlayerName))
        {
            Debug.LogError("[NetworkPlayerProfileManager] Player name not set. Cannot set class.");
            return;
        }

        PlayerStats loaded = LoadStatsFromDatabase(forPlayerName);
        if (loaded == null)
        {
            Debug.LogError("[NetworkPlayerProfileManager] Failed to load stats for SetPlayerClass.");
            return;
        }

        loaded.className = className;
        loaded.hasSelectedClass = true;
        SaveStatsToDatabase(loaded);
        Debug.Log($"[NetworkPlayerProfileManager] Class set to {className} for {forPlayerName}");
    }

    public PlayerStats LoadStatsFromDatabase(string forPlayerName)
    {
        if (string.IsNullOrEmpty(forPlayerName))
        {
            Debug.LogError("[NetworkPlayerProfileManager] playerName empty.");
            return null;
        }

        if (DBConnectionManager.Instance == null)
        {
            Debug.LogError("[NetworkPlayerProfileManager] DBConnectionManager.Instance is null.");
            return null;
        }

        PlayerStats loaded = DBConnectionManager.Instance.LoadPlayerStats(forPlayerName);
        if (loaded != null)
            _playerStats = loaded;
        else
            Debug.LogWarning($"[NetworkPlayerProfileManager] No stats for {forPlayerName}");

        return loaded;
    }

    private void SaveStatsToDatabase(PlayerStats stats)
    {
        if (stats == null)
        {
            Debug.LogWarning("[NetworkPlayerProfileManager] No stats to save.");
            return;
        }

        if (DBConnectionManager.Instance == null)
        {
            Debug.LogError("[NetworkPlayerProfileManager] DBConnectionManager.Instance is null.");
            return;
        }

        DBConnectionManager.Instance.SaveToDB(stats);
    }

    public PlayerStats GetPlayerStats() => _playerStats;

    public bool IsPlayerProfileComplete()
    {
        return _playerStats != null
               && !string.IsNullOrEmpty(_playerStats.className)
               && !string.IsNullOrEmpty(_playerStats.faction);
    }
}
