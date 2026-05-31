using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Area Data")]
public class AreaData : ScriptableObject
{
    public string areaId;
    public string areaName;
    public Vector2 areaIndex;

    // Existing permanent enemies
    public List<EnemyEntry> enemies = new();

    // NEW: Random encounter system
    public bool enableRandomEncounters = false;
    public List<RandomEnemyEntry> randomEncounterPool = new();
    public float randomEncounterChance = 0.1f; // Base chance per movement tick
    public float minDistanceBetweenEncounters = 5f; // Minimum distance before another can spawn

    public CharacterData PickRandomEnemy()
    {

        CharacterData randomCharacter;

        int charIndex = Random.Range(0, enemies.Count);

        randomCharacter = randomEncounterPool[charIndex].enemyData; 

        return randomCharacter; 
    }



}

[System.Serializable]
public class EnemyEntry
{
    public CharacterData enemyData;   // ScriptableObject with stats + prefab
    public string spawnPointId;       // Matches ID in SpawnPointRegistry
}

[System.Serializable]
public class RandomEnemyEntry
{
    public CharacterData enemyData;           // The enemy to spawn
    public float weight = 1f;                // Higher weight = more common
    public int minPlayerLevel = 1;           // Minimum player level for this enemy
    public int maxPlayerLevel = 99;          // Maximum player level for this enemy
    public List<string> requiredFlags = new(); // Optional: only spawn if these story flags are set
}