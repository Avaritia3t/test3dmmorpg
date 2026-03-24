using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Mono.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

/// <summary>
/// Local SQLite persistence (Mono.Data.Sqlite) matching the DarkSkiesV0.03 pattern: <c>UserAccounts</c> + <c>PlayerStats</c>.
/// <para><b>Mirror / server authority:</b> This is a <b>client-side</b> profile store for offline / ParrelSync prototyping.
/// Production login and stat truth should live on the server (e.g. <see cref="Mirror.NetworkServer"/> + your auth service).</para>
/// <para>Requires: <c>com.unity.nuget.newtonsoft-json</c> and SQLite plugins (e.g. <c>Mono.Data.Sqlite</c>) in the Unity project.</para>
/// </summary>
public class DBConnectionManager : MonoBehaviour
{
    public static DBConnectionManager Instance { get; private set; }

    private string _dbPath;

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        ContractResolver = new IgnoreUnityObjectResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _dbPath = "URI=file:" + Application.persistentDataPath + "/PlayerStatsDB.sqlite";
            CreateDB();
        }
        else
        {
            Destroy(gameObject);
            Debug.LogWarning($"{nameof(DBConnectionManager)} duplicate destroyed.");
        }
    }

    private void CreateDB()
    {
        using (var connection = new SqliteConnection(_dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GenerateCreatePlayerStatsTableQuery();
                command.ExecuteNonQuery();

                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS UserAccounts (
                        Username TEXT PRIMARY KEY,
                        PasswordHash TEXT NOT NULL,
                        PlayerStatsId INTEGER NOT NULL,
                        FOREIGN KEY(PlayerStatsId) REFERENCES PlayerStats(Id)
                    );";
                command.ExecuteNonQuery();
            }
        }
    }

    private static string GenerateCreatePlayerStatsTableQuery()
    {
        var sb = new StringBuilder("CREATE TABLE IF NOT EXISTS PlayerStats (Id INTEGER PRIMARY KEY AUTOINCREMENT, ");
        foreach (var prop in GetPersistedProperties())
            sb.Append(prop.Name).Append(' ').Append(GetSqlType(prop.PropertyType)).Append(", ");

        sb.Append("equipmentSlotsJson TEXT");
        sb.Append(");");
        return sb.ToString();
    }

    private static IEnumerable<PropertyInfo> GetPersistedProperties()
    {
        foreach (var p in typeof(PlayerStats).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite)
                continue;
            if (p.GetIndexParameters().Length > 0)
                continue;
            if (!IsScalarPersistType(p.PropertyType))
                continue;
            yield return p;
        }
    }

    private static bool IsScalarPersistType(Type t)
    {
        if (t == typeof(string) || t == typeof(int) || t == typeof(float) || t == typeof(bool) ||
            t == typeof(double) || t == typeof(long) || t == typeof(uint))
            return true;
        return false;
    }

    private static string GetSqlType(Type type)
    {
        if (type == typeof(string)) return "TEXT";
        if (type == typeof(int) || type == typeof(long) || type == typeof(uint)) return "INTEGER";
        if (type == typeof(float) || type == typeof(double)) return "REAL";
        if (type == typeof(bool)) return "INTEGER";
        return "TEXT";
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password ?? "");
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    public void CreateUser(string username, string password)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentException("username required", nameof(username));

        var defaultStats = new PlayerStats { playerName = username };

        string insertStats = GenerateInsertPlayerStatsQuery(defaultStats);
        string hash = HashPassword(password);

        using (var connection = new SqliteConnection(_dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                try
                {
                    command.CommandText = insertStats;
                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid()";
                    long playerStatsId = (long)command.ExecuteScalar();

                    command.Parameters.Clear();
                    command.CommandText = @"
                        INSERT INTO UserAccounts (Username, PasswordHash, PlayerStatsId)
                        VALUES (@Username, @PasswordHash, @PlayerStatsId)";
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", hash);
                    command.Parameters.AddWithValue("@PlayerStatsId", playerStatsId);
                    command.ExecuteNonQuery();

                    Debug.Log($"[DBConnectionManager] User created: {username}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DBConnectionManager] CreateUser failed: {ex.Message}");
                }
            }
        }
    }

    private static string GenerateInsertPlayerStatsQuery(PlayerStats stats)
    {
        var columns = new List<string>();
        var values = new List<string>();

        foreach (var prop in GetPersistedProperties())
        {
            columns.Add(prop.Name);
            values.Add(FormatSqlValue(prop.GetValue(stats), prop.PropertyType));
        }

        columns.Add("equipmentSlotsJson");
        values.Add("'" + EscapeSql(SerializeEquipmentSlots(stats.equipmentSlots)) + "'");

        return $"INSERT INTO PlayerStats ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});";
    }

    private static string SerializeEquipmentSlots(List<EquipmentSlot> slots)
    {
        if (slots == null)
            return "";
        return JsonConvert.SerializeObject(slots, JsonSettings);
    }

    private static List<EquipmentSlot> DeserializeEquipmentSlots(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonConvert.DeserializeObject<List<EquipmentSlot>>(json, JsonSettings);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DBConnectionManager] equipmentSlotsJson parse failed: {e.Message}");
            return null;
        }
    }

    private static string FormatSqlValue(object value, Type type)
    {
        if (value == null)
            return "NULL";
        if (type == typeof(string))
            return "'" + EscapeSql((string)value) + "'";
        if (type == typeof(bool))
            return (bool)value ? "1" : "0";
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static string EscapeSql(string s) => (s ?? "").Replace("'", "''");

    public bool ValidateLogin(string username, string password)
    {
        string hash = HashPassword(password);
        using (var connection = new SqliteConnection(_dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT PasswordHash FROM UserAccounts WHERE Username = @Username";
                command.Parameters.AddWithValue("@Username", username);
                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string stored = reader.GetString(0);
                        return stored == hash;
                    }
                }
            }
        }

        return false;
    }

    public PlayerStats LoadPlayerStats(string username)
    {
        if (string.IsNullOrEmpty(username))
            return null;

        using (var connection = new SqliteConnection(_dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT PlayerStatsId FROM UserAccounts WHERE Username = @Username";
                command.Parameters.AddWithValue("@Username", username);
                object result = command.ExecuteScalar();
                if (result == null)
                    return null;

                long playerStatsId = (long)result;
                command.Parameters.Clear();
                command.CommandText = "SELECT * FROM PlayerStats WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", playerStatsId);

                using (IDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    var stats = new PlayerStats();
                    foreach (var prop in GetPersistedProperties())
                    {
                        int ord;
                        try
                        {
                            ord = reader.GetOrdinal(prop.Name);
                        }
                        catch
                        {
                            continue;
                        }

                        if (reader.IsDBNull(ord))
                            continue;

                        object raw = reader.GetValue(ord);
                        try
                        {
                            if (prop.PropertyType == typeof(float))
                                prop.SetValue(stats, Convert.ToSingle(raw));
                            else if (prop.PropertyType == typeof(double))
                                prop.SetValue(stats, Convert.ToDouble(raw));
                            else if (prop.PropertyType == typeof(int))
                                prop.SetValue(stats, Convert.ToInt32(raw));
                            else if (prop.PropertyType == typeof(long))
                                prop.SetValue(stats, Convert.ToInt64(raw));
                            else if (prop.PropertyType == typeof(uint))
                                prop.SetValue(stats, Convert.ToUInt32(raw));
                            else if (prop.PropertyType == typeof(bool))
                                prop.SetValue(stats, Convert.ToInt32(raw) != 0);
                            else if (prop.PropertyType == typeof(string))
                                prop.SetValue(stats, reader.GetString(ord));
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[DBConnectionManager] Failed to map {prop.Name}: {e.Message}");
                        }
                    }

                    int eqOrd;
                    try
                    {
                        eqOrd = reader.GetOrdinal("equipmentSlotsJson");
                    }
                    catch
                    {
                        eqOrd = -1;
                    }

                    if (eqOrd >= 0 && !reader.IsDBNull(eqOrd))
                    {
                        var list = DeserializeEquipmentSlots(reader.GetString(eqOrd));
                        if (list != null)
                            stats.equipmentSlots = list;
                    }

                    return stats;
                }
            }
        }
    }

    public void SaveToDB(PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            Debug.LogError("[DBConnectionManager] SaveToDB: null stats.");
            return;
        }

        using (var connection = new SqliteConnection(_dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM PlayerStats WHERE playerName = @PlayerName";
                command.Parameters.AddWithValue("@PlayerName", playerStats.playerName ?? "");
                object idResult = command.ExecuteScalar();
                if (idResult == null)
                {
                    Debug.LogError($"[DBConnectionManager] No PlayerStats row for playerName={playerStats.playerName}");
                    return;
                }

                long id = (long)idResult;

                var sb = new StringBuilder("UPDATE PlayerStats SET ");
                command.Parameters.Clear();

                foreach (var prop in GetPersistedProperties())
                {
                    if (prop.Name == "playerName")
                        continue;
                    sb.Append(prop.Name).Append(" = @").Append(prop.Name).Append(", ");
                    command.Parameters.AddWithValue("@" + prop.Name, BoxForDb(prop.GetValue(playerStats), prop.PropertyType));
                }

                sb.Append("equipmentSlotsJson = @equipmentSlotsJson");
                command.Parameters.AddWithValue("@equipmentSlotsJson", SerializeEquipmentSlots(playerStats.equipmentSlots));

                sb.Append(" WHERE Id = @Id");
                command.Parameters.AddWithValue("@Id", id);
                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }
        }
    }

    private static object BoxForDb(object value, Type t)
    {
        if (value == null)
            return DBNull.Value;
        if (t == typeof(bool))
            return (bool)value ? 1 : 0;
        return value;
    }

    /// <summary>Skips UnityEngine.Object-derived members so <see cref="Item.icon"/> etc. are not serialized.</summary>
    private sealed class IgnoreUnityObjectResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var p = base.CreateProperty(member, memberSerialization);
            if (typeof(UnityEngine.Object).IsAssignableFrom(p.PropertyType))
                p.Ignored = true;
            return p;
        }
    }
}
