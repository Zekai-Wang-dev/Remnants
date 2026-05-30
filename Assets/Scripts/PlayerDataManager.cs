using UnityEngine;
using System.IO;

public class SaveSystem
{
    public static void SavePlayerData(PlayerData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Application.persistentDataPath + "/playerData.json", json);
    }

    public static PlayerData LoadPlayerData()
    {
        string path = Application.persistentDataPath + "/playerData.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerData>(json);
        }
        return null;
    }
}

[System.Serializable]
public class PlayerData
{
    // Base stats (unchanging unless upgraded)
    public float baseMaxHealth;
    public float baseSpeed;
    public float baseDamage;
    public float baseTrueDefense;
    public float baseAccuracy;
    public float baseBlockBypassRate;
    public float baseDodgeBypassRate;
    public float baseEnergyReserves;
    public float baseParryChance;
    public float baseBlockChance;
    public float baseDodgeChance;

    // Current stats (change during gameplay)
    public float currentHealth;
    public float currentSpeed;
    public float currentDamage;
    public float currentTrueDefense;
    public float currentAccuracy;
    public float currentBlockBypassRate;
    public float currentDodgeBypassRate;
    public float currentEnergyReserves;
    public float currentParryChance;
    public float currentBlockChance;
    public float currentDodgeChance;

    // Derived stats (calculated)
    public float MaxHealth => baseMaxHealth;
    public float Speed => baseSpeed;
    public float Damage => baseDamage;

    // Reset current stats to base values
    public void ResetToBaseStats()
    {
        currentHealth = baseMaxHealth;
        currentSpeed = baseSpeed;
        currentDamage = baseDamage;
        currentTrueDefense = baseTrueDefense;
        currentAccuracy = baseAccuracy;
        currentBlockBypassRate = baseBlockBypassRate;
        currentDodgeBypassRate = baseDodgeBypassRate;
        currentEnergyReserves = baseEnergyReserves;
        currentParryChance = baseParryChance;
        currentBlockChance = baseBlockChance;
        currentDodgeChance = baseDodgeChance;
    }

    // Apply damage
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }

    // Heal
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(baseMaxHealth, currentHealth + amount);
    }

}
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Base Stats (Configurable in Inspector)")]
    public float baseMaxHealth = 500f;
    public float baseSpeed = 5f;
    public float baseDamage = 10f;
    public float baseTrueDefense = 5f;
    public float baseAccuracy = 0.8f;
    public float baseBlockBypassRate = 0.1f;
    public float baseDodgeBypassRate = 0.1f;
    public float baseEnergyReserves = 10f;
    public float baseParryChance = 0.1f;
    public float baseBlockChance = 0.2f;
    public float baseDodgeChance = 0.15f;
    // ... other base stats

    [Header("Runtime Data")]
    public PlayerData playerData;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePlayerData();
    }

    void InitializePlayerData()
    {
        playerData = new PlayerData();

        // Set base stats
        playerData.baseMaxHealth = baseMaxHealth;
        playerData.baseSpeed = baseSpeed;
        playerData.baseDamage = baseDamage;
        // ... set all base stats

        // Initialize current stats to base values
        playerData.ResetToBaseStats();
    }
}