using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Area Data")]
public class AreaData : ScriptableObject
{
    public string areaId;
    public string areaName;
    public Vector2 areaIndex;

    public List<EnemyEntry> enemies = new();

    public bool enableRandomEncounters = false;
    public List<RandomEnemyEntry> randomEncounterPool = new();
    public float randomEncounterChance = 0.1f;
    public float minDistanceBetweenEncounters = 5f;

    public List<InteractableEntry> interactables = new();

    public string Id => GameIdUtility.GetAreaId(this);

    private void OnValidate()
    {
        areaId = GameIdUtility.CleanOrCreate(areaId, this);
    }

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
                        if (!activeFlags.Contains(GameIdUtility.Normalize(flag)))
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
        if (string.IsNullOrWhiteSpace(encounterId) || enemies == null) return null;

        string normalizedEncounterId = GameIdUtility.Normalize(encounterId);

        foreach (EnemyEntry entry in enemies)
        {
            if (entry == null || entry.enemyData == null) continue;

            if (GameIdUtility.Normalize(entry.spawnPointId) == normalizedEncounterId)
            {
                return entry.enemyData;
            }
        }

        return null;
    }

    public CharacterData FindRandomEnemyByEncounterId(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(encounterId) || randomEncounterPool == null) return null;

        string enemyId = GameIdUtility.Normalize(encounterId, true);

        foreach (RandomEnemyEntry entry in randomEncounterPool)
        {
            if (entry == null || entry.enemyData == null) continue;

            string characterId = GameIdUtility.GetCharacterId(entry.enemyData);
            string assetNameId = GameIdUtility.ToStableId(entry.enemyData.name);
            string charTypeId = GameIdUtility.Normalize(entry.enemyData.charType);

            if (enemyId == characterId || enemyId == assetNameId || enemyId == charTypeId)
            {
                return entry.enemyData;
            }
        }

        return null;
    }
}

[System.Serializable]
public class InteractableEntry
{
    public InteractableData interactableData;
    public string spawnPointId;
}

[System.Serializable]
public class EnemyEntry
{
    public CharacterData enemyData;
    public string spawnPointId;
}

[System.Serializable]
public class RandomEnemyEntry
{
    public CharacterData enemyData;
    public float weight = 1f;
    public int minPlayerLevel = 1;
    public int maxPlayerLevel = 99;
    public List<string> requiredFlags = new();
}
