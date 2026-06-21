using System.Collections.Generic;
using UnityEngine;

public class SpawnPointRegistry : MonoBehaviour
{
    public static SpawnPointRegistry Instance { get; private set; }

    [System.Serializable]
    public class SpawnPoint
    {
        public string areaId;
        public Transform playerSpawn;
        public Transform leftBorder;
        public Transform rightBorder;
        public List<EnemySpawn> enemySpawns = new();
        public List<InteractableSpawn> interactableSpawns = new();
    }

    [System.Serializable]
    public class EnemySpawn
    {
        public string spawnPointId;
        public Transform transform;
    }

    [System.Serializable]
    public class InteractableSpawn
    {
        public string spawnPointId;
        public Transform transform;
    }

    public List<SpawnPoint> spawnPoints = new();
    private Dictionary<string, SpawnPoint> lookup = new();

    private void Awake()
    {
        Instance = this;
        lookup.Clear();
        foreach (var sp in spawnPoints)
            lookup[sp.areaId] = sp;
    }

    public SpawnPoint Get(string areaId)
    {
        lookup.TryGetValue(areaId, out var sp);
        return sp;
    }
}