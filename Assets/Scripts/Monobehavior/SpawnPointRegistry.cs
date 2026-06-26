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
        RebuildLookup();
    }

    private void OnValidate()
    {
        if (spawnPoints == null) return;

        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp == null) continue;

            sp.areaId = GameIdUtility.Normalize(sp.areaId);

            if (sp.enemySpawns != null)
            {
                foreach (EnemySpawn enemySpawn in sp.enemySpawns)
                {
                    if (enemySpawn == null) continue;
                    enemySpawn.spawnPointId = GameIdUtility.Normalize(enemySpawn.spawnPointId);
                }
            }

            if (sp.interactableSpawns != null)
            {
                foreach (InteractableSpawn interactableSpawn in sp.interactableSpawns)
                {
                    if (interactableSpawn == null) continue;
                    interactableSpawn.spawnPointId = GameIdUtility.Normalize(interactableSpawn.spawnPointId);
                }
            }
        }
    }

    public void RebuildLookup()
    {
        lookup.Clear();

        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp == null) continue;

            string areaId = GameIdUtility.Normalize(sp.areaId);
            if (string.IsNullOrWhiteSpace(areaId)) continue;

            lookup[areaId] = sp;
        }
    }

    public SpawnPoint Get(string areaId)
    {
        if (lookup == null || lookup.Count == 0)
        {
            RebuildLookup();
        }

        lookup.TryGetValue(GameIdUtility.Normalize(areaId), out SpawnPoint sp);
        return sp;
    }
}
