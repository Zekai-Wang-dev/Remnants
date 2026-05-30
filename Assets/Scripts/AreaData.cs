using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Area Data")]
public class AreaData : ScriptableObject
{
    public string areaId;
    public string areaName;
    public Vector2 areaIndex;
    public List<EnemyEntry> enemies = new();
}

[System.Serializable]
public class EnemyEntry
{
    public CharacterData enemyData;   // ScriptableObject with stats + prefab
    public string spawnPointId;   // Matches ID in SpawnPointRegistry
}