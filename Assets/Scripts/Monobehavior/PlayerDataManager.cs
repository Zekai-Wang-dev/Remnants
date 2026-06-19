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
            Debug.Log(path);
            return JsonUtility.FromJson<PlayerData>(json);
        }

        return null;
    }
}

[System.Serializable]
public class PlayerData
{
    public string characterDataName;

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

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }
}

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Player Character Data")]
    public CharacterData playerCharacterData;

    [Header("Runtime Data")]
    public PlayerData playerData;

    void Awake()
    {
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
        playerData = SaveSystem.LoadPlayerData();

        if (playerData == null)
        {
            playerData = new PlayerData();

            if (playerCharacterData != null)
            {
                playerData.characterDataName = playerCharacterData.name;
                playerData.ResetToBaseStats(playerCharacterData);
            }
            else
            {
                Debug.LogError("PlayerDataManager has no playerCharacterData assigned.");
            }
        }
        else if (playerCharacterData != null && playerData.characterDataName != playerCharacterData.name)
        {
            Debug.LogWarning("Saved character data does not match current CharacterData. Resetting to base stats.");
            playerData.characterDataName = playerCharacterData.name;
            playerData.ResetToBaseStats(playerCharacterData);
        }
    }

    public void HealPlayer(float amount)
    {
        if (playerCharacterData == null || playerData == null) return;

        playerData.currentHealth = Mathf.Min(
            playerCharacterData.maxHealth,
            playerData.currentHealth + amount);
    }

    public void DamagePlayer(float damage)
    {
        if (playerData == null) return;
        playerData.TakeDamage(damage);
    }

    public void ResetPlayerToBaseStats()
    {
        if (playerData == null || playerCharacterData == null) return;
        playerData.ResetToBaseStats(playerCharacterData);
    }

    public void SavePlayer()
    {
        if (playerData == null) return;
        SaveSystem.SavePlayerData(playerData);
    }

    public void LoadPlayer()
    {
        playerData = SaveSystem.LoadPlayerData();
        if (playerData == null)
        {
            InitializePlayerData();
        }
    }

    public float GetHealthPercentage()
    {
        if (playerCharacterData == null || playerData == null) return 0f;
        return playerData.currentHealth / playerCharacterData.maxHealth;
    }

    public bool IsPlayerAlive()
    {
        return playerData != null && playerData.currentHealth > 0;
    }

    public void RecalculateStatsFromEquipment()
    {
        if (playerData == null || playerCharacterData == null)
        {
            return;
        }

        float oldHealth = playerData.currentHealth;

        playerData.ResetToBaseStats(playerCharacterData);

        ApplyEquippedItemStats();

        // Optional: preserve current health instead of fully healing on every equipment change.
        playerData.currentHealth = Mathf.Min(oldHealth, playerData.currentHealth);
    }

    private void ApplyEquippedItemStats()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        foreach (InventorySlotData slot in InventoryManager.Instance.inventory)
        {
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                continue;
            }

            if (!slot.equipped)
            {
                continue;
            }

            ItemData item = slot.item;

            if (item.itemType == ItemType.Weapon)
            {
                ApplyWeaponStats(item.weaponData);
            }
            else if (item.itemType == ItemType.Equipment)
            {
                ApplyEquipmentStats(item.equipmentData);
            }
        }
    }

    private void ApplyWeaponStats(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            return;
        }

        playerData.currentDamage += weaponData.damage;
        playerData.currentSpeed += weaponData.speed;
        playerData.currentAccuracy += weaponData.accuracy;
        playerData.currentBlockBypassRate += weaponData.blockBypassRate;
        playerData.currentDodgeBypassRate += weaponData.dodgeBypassRate;
        playerData.currentParryChance += weaponData.parryChance;
        playerData.currentBlockChance += weaponData.blockChance;
        playerData.currentEnergyReserves += weaponData.energyReserves;
    }

    private void ApplyEquipmentStats(EquipmentData equipmentData)
    {
        if (equipmentData == null)
        {
            return;
        }

        playerData.currentSpeed += equipmentData.speed;
        playerData.currentHealth += equipmentData.maxHealth;
        playerData.currentBaseDefense += equipmentData.baseDefense;
        playerData.currentTrueDefense += equipmentData.trueDefense;
        playerData.currentEnergyReserves += equipmentData.energyReserves;
    }
}
