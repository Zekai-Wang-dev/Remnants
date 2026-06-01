using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Area Data")]
public class AreaData : ScriptableObject
{
    public string areaId;
    public string areaName;
    public Vector2 areaIndex;

    // Permanent / fixed encounter enemies.
    public List<EnemyEntry> enemies = new();

    // Random encounter system.
    public bool enableRandomEncounters = false;
    public List<RandomEnemyEntry> randomEncounterPool = new();
    public float randomEncounterChance = 0.1f; // Base chance per movement check.
    public float minDistanceBetweenEncounters = 5f; // Minimum distance before another can spawn.

    /// <summary>
    /// Picks one valid random enemy using weights.
    /// Keeps the old no-argument call working, but also supports level/flag filtering later.
    /// </summary>
    public CharacterData PickRandomEnemy(int playerLevel = 1, HashSet<string> activeFlags = null)
    {
        if (randomEncounterPool == null || randomEncounterPool.Count == 0)
        {
            Debug.LogWarning($"AreaData '{name}' has no random enemies in its randomEncounterPool.");
            return null;
        }

        List<RandomEnemyEntry> validEntries = new();

        foreach (RandomEnemyEntry entry in randomEncounterPool)
        {
            if (entry == null || entry.enemyData == null) continue;
            if (entry.weight <= 0f) continue;
            if (playerLevel < entry.minPlayerLevel || playerLevel > entry.maxPlayerLevel) continue;

            bool hasRequiredFlags = true;
            if (entry.requiredFlags != null && entry.requiredFlags.Count > 0)
            {
                if (activeFlags == null)
                {
                    hasRequiredFlags = false;
                }
                else
                {
                    foreach (string flag in entry.requiredFlags)
                    {
                        if (!activeFlags.Contains(flag))
                        {
                            hasRequiredFlags = false;
                            break;
                        }
                    }
                }
            }

            if (hasRequiredFlags)
            {
                validEntries.Add(entry);
            }
        }

        if (validEntries.Count == 0)
        {
            Debug.LogWarning($"AreaData '{name}' found no valid random enemy for level {playerLevel}. Falling back to first valid pool entry.");

            foreach (RandomEnemyEntry entry in randomEncounterPool)
            {
                if (entry != null && entry.enemyData != null)
                {
                    return entry.enemyData;
                }
            }

            return null;
        }

        float totalWeight = 0f;
        foreach (RandomEnemyEntry entry in validEntries)
        {
            totalWeight += Mathf.Max(0f, entry.weight);
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (RandomEnemyEntry entry in validEntries)
        {
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                return entry.enemyData;
            }
        }

        return validEntries[validEntries.Count - 1].enemyData;
    }

    public CharacterData FindFixedEnemyByEncounterId(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId) || enemies == null) return null;

        foreach (EnemyEntry entry in enemies)
        {
            if (entry != null && entry.spawnPointId == encounterId)
            {
                return entry.enemyData;
            }
        }

        return null;
    }

    public CharacterData FindRandomEnemyByEncounterId(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId) || randomEncounterPool == null) return null;

        const string randomPrefix = "random_";
        string enemyName = encounterId.StartsWith(randomPrefix)
            ? encounterId.Substring(randomPrefix.Length)
            : encounterId;

        foreach (RandomEnemyEntry entry in randomEncounterPool)
        {
            if (entry == null || entry.enemyData == null) continue;

            if (entry.enemyData.name == enemyName || entry.enemyData.charType == enemyName)
            {
                return entry.enemyData;
            }
        }

        return null;
    }
}

[System.Serializable]
public class EnemyEntry
{
    public CharacterData enemyData;   // ScriptableObject with stats + prefab.
    public string spawnPointId;       // Matches ID in SpawnPointRegistry / EncounterTrigger.
}

[System.Serializable]
public class RandomEnemyEntry
{
    public CharacterData enemyData;             // The enemy to spawn.
    public float weight = 1f;                   // Higher weight = more common.
    public int minPlayerLevel = 1;              // Minimum player level for this enemy.
    public int maxPlayerLevel = 99;             // Maximum player level for this enemy.
    public List<string> requiredFlags = new();  // Optional: only spawn if story flags are set.
}
