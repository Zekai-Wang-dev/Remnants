using UnityEngine;
using System.IO;
using Unity.VisualScripting.FullSerializer;

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
            Debug.Log(path);
            return JsonUtility.FromJson<PlayerData>(json);
        }
        return null;
    }
}

[System.Serializable]
public class PlayerData
{
    // Reference to the CharacterData asset name (for loading)
    public string characterDataName;

    // Current stats (change during gameplay)
    public float currentHealth;
    public float currentSpeed;
    public float currentDamage;
    public float currentBaseDefense;
    public float currentTrueDefense;
    public float currentAccuracy;
    public float currentBlockBypassRate;
    public float currentDodgeBypassRate;
    public float currentEnergyReserves;
    public float currentParryChance;
    public float currentBlockChance;
    public float currentDodgeChance;

    // Reset current stats to base values from CharacterData
    public void ResetToBaseStats(CharacterData characterData)
    {
        if (characterData == null) return;

        currentHealth = characterData.maxHealth;
        currentSpeed = characterData.speed;
        currentDamage = characterData.damage;
        currentBaseDefense = characterData.defense;
        currentTrueDefense = characterData.trueDefense;
        currentAccuracy = characterData.accuracy;
        currentBlockBypassRate = characterData.blockBypassRate;
        currentDodgeBypassRate = characterData.dodgeBypassRate;
        currentEnergyReserves = characterData.energyReserves;
        currentParryChance = characterData.parryChance;
        currentBlockChance = characterData.blockChance;
        currentDodgeChance = characterData.dodgeChance;
    }

    // Apply damage
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }

    // Heal
    public void Heal(float amount)
    {
        // We need to know max health from CharacterData, but we don't have it here
        // We'll handle this in PlayerDataManager
    }
}

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Player Character Data")]
    public CharacterData playerCharacterData; // Assign in inspector

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
        // Try to load saved data
        playerData = SaveSystem.LoadPlayerData();

        if (playerData == null)
        {
            // No saved data, create new
            playerData = new PlayerData();
            playerData.characterDataName = playerCharacterData.name;
            playerData.ResetToBaseStats(playerCharacterData);
        }
        else
        {
            // Verify the saved data matches current CharacterData
            if (playerData.characterDataName != playerCharacterData.name)
            {
                Debug.LogWarning("Saved character data doesn't match current CharacterData. Resetting to base stats.");
                playerData.ResetToBaseStats(playerCharacterData);
            }
        }
    }

    // Public method to heal player (needs max health from CharacterData)
    public void HealPlayer(float amount)
    {
        if (playerCharacterData == null) return;

        playerData.currentHealth = Mathf.Min(playerCharacterData.maxHealth,
                                             playerData.currentHealth + amount);
    }

    // Public method to take damage
    public void DamagePlayer(float damage)
    {
        playerData.TakeDamage(damage);
    }

    // Public method to reset player to base stats
    public void ResetPlayerToBaseStats()
    {
        playerData.ResetToBaseStats(playerCharacterData);
    }

    // Save player data
    public void SavePlayer()
    {
        SaveSystem.SavePlayerData(playerData);
    }

    // Load player data
    public void LoadPlayer()
    {
        playerData = SaveSystem.LoadPlayerData();
        if (playerData == null)
        {
            // If no save exists, initialize fresh
            InitializePlayerData();
        }
    }

    // Get current player health percentage
    public float GetHealthPercentage()
    {
        if (playerCharacterData == null) return 0f;
        return playerData.currentHealth / playerCharacterData.maxHealth;
    }

    // Check if player is alive
    public bool IsPlayerAlive()
    {
        return playerData.currentHealth > 0;
    }
}