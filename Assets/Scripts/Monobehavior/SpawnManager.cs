using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Scene
{
    public string sceneName;
    public string sceneId;
    public Vector2 sceneIndex;
    public List<AreaData> areas;
}

public class SpawnManager : MonoBehaviour
{
    public List<Scene> scenes;
    public string currentSceneId;
    public string currentAreaId;
    public Vector2 currentAreaIndex;
    public Vector2 currentSceneIndex;
    public string previousSceneId;
    public string previousAreaId;
    public Vector2 previousAreaIndex;
    public Vector2 previousSceneIndex;

    private readonly Dictionary<string, Vector2> areaPositions = new();
    private SpawnPointRegistry spawnPointRegistry;
    private readonly Dictionary<string, HashSet<string>> defeatedEncounters = new();

    public string currentEncounterAreaId;
    public string currentEncounterId;

    private static SpawnManager _instance;
    public static SpawnManager Instance => _instance;

    private CameraController GetCameraController()
    {
        return Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        _instance = this;
        LoadDefeatedEncounters();
    }

    private void Start()
    {
        spawnPointRegistry = Object.FindFirstObjectByType<SpawnPointRegistry>();
        if (spawnPointRegistry == null)
        {
            Debug.LogError("SpawnPointRegistry not found in scene!");
        }

        LoadScene(currentSceneId, currentAreaId);
    }

    public void SetCurrentEncounter(string areaId, string encounterId)
    {
        currentEncounterAreaId = areaId;
        currentEncounterId = encounterId;
    }

    public void ClearCurrentEncounter()
    {
        currentEncounterAreaId = "";
        currentEncounterId = "";
    }

    public int FindSceneIndex(string sceneId)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneId == sceneId)
            {
                return i;
            }
        }

        return 0;
    }

    public int FindAreaIndex(string areaId)
    {
        int sceneIdx = FindSceneIndex(currentSceneId);

        if (sceneIdx < 0 || sceneIdx >= scenes.Count || scenes[sceneIdx].areas == null)
        {
            return 0;
        }

        for (int i = 0; i < scenes[sceneIdx].areas.Count; i++)
        {
            if (scenes[sceneIdx].areas[i].areaId == areaId)
            {
                return i;
            }
        }

        return 0;
    }

    public void UpdateCurrentIdInfo()
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneIndex == currentSceneIndex)
            {
                currentSceneId = scenes[i].sceneId;
                break;
            }
        }

        int sceneIdx = FindSceneIndex(currentSceneId);
        if (sceneIdx >= 0 && sceneIdx < scenes.Count && scenes[sceneIdx].areas != null)
        {
            for (int i = 0; i < scenes[sceneIdx].areas.Count; i++)
            {
                if (scenes[sceneIdx].areas[i].areaIndex == currentAreaIndex)
                {
                    currentAreaId = scenes[sceneIdx].areas[i].areaId;
                    break;
                }
            }
        }

        if (currentAreaId == previousAreaId)
        {
            currentAreaIndex = previousAreaIndex;
        }

        if (currentSceneId == previousSceneId)
        {
            currentSceneIndex = previousSceneIndex;
        }
    }

    public void NextArea(string direction)
    {
        StoreCurrentAreaPosition();

        previousAreaIndex = currentAreaIndex;
        previousAreaId = currentAreaId;

        switch (direction)
        {
            case "Left":
                currentAreaIndex.x--;
                break;
            case "Right":
                currentAreaIndex.x++;
                break;
            case "Forward":
                currentAreaIndex.y++;
                break;
            case "Backward":
                currentAreaIndex.y--;
                break;
        }

        UpdateCurrentIdInfo();
        LoadArea(currentSceneId, currentAreaId);
        GetCameraController()?.changeBorders();
    }

    public void NextScene(string direction)
    {
        StoreCurrentAreaPosition();

        if (currentSceneId != "001")
        {
            previousSceneIndex = currentSceneIndex;
            previousSceneId = currentSceneId;
            previousAreaIndex = currentAreaIndex;
            previousAreaId = currentAreaId;
        }

        switch (direction)
        {
            case "Left":
                currentSceneIndex.x--;
                break;
            case "Right":
                currentSceneIndex.x++;
                break;
            case "Forward":
                currentSceneIndex.y++;
                break;
            case "Backward":
                currentSceneIndex.y--;
                break;
            case "Combat":
                currentSceneIndex = new Vector2(1, 0);
                currentAreaIndex = new Vector2(0, 0);
                SetPlayerVisibility(false);
                break;
            case "CombatOver":
                currentSceneIndex = previousSceneIndex;
                currentAreaIndex = previousAreaIndex;
                SetPlayerVisibility(true);
                break;
        }

        UpdateCurrentIdInfo();
        LoadScene(currentSceneId, currentAreaId);
        GetCameraController()?.changeBorders();
    }

    private void SetPlayerVisibility(bool visible)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = visible;

        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null) col.enabled = visible;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = visible;
    }

    private void StoreCurrentAreaPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            areaPositions[currentAreaId] = playerRb.position;
        }
    }

    public void LoadArea(string sceneId, string areaId)
    {
        Scene sceneToLoad = scenes.Find(s => s.sceneId == sceneId);
        if (sceneToLoad == null) return;

        AreaData areaData = sceneToLoad.areas.Find(a => a.areaId == areaId);
        if (areaData == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        if (spawnPointRegistry == null)
        {
            spawnPointRegistry = Object.FindFirstObjectByType<SpawnPointRegistry>();
        }

        SpawnPointRegistry.SpawnPoint spawnPoints = spawnPointRegistry != null ? spawnPointRegistry.Get(areaId) : null;
        if (spawnPoints == null)
        {
            Debug.LogError($"No spawn points found for area {areaId}");
            return;
        }

        UpdateParallaxArea(areaData);

        if (areaPositions.ContainsKey(areaId))
        {
            playerRb.position = areaPositions[areaId];
        }
        else
        {
            playerRb.position = spawnPoints.playerSpawn.position;
        }

        CameraController cam = GetCameraController();
        if (cam != null)
        {
            cam.SetBorders(spawnPoints.leftBorder, spawnPoints.rightBorder);
        }

        SpawnEnemies(areaData, spawnPoints);
        CleanupDefeatedEncounters(areaId);
    }

    public void LoadScene(string sceneId, string areaId)
    {
        Scene sceneToLoad = scenes.Find(s => s.sceneId == sceneId);
        if (sceneToLoad == null)
        {
            Debug.Log("Cannot Find Scene with id: " + sceneId);
            return;
        }

        AreaData areaData = sceneToLoad.areas.Find(a => a.areaId == areaId);
        if (areaData == null)
        {
            Debug.Log("Cannot Find Area with Id: " + areaId);
            return;
        }

        SceneManager.LoadSceneAsync(sceneToLoad.sceneName).completed += (_) =>
        {
            spawnPointRegistry = Object.FindFirstObjectByType<SpawnPointRegistry>();
            if (spawnPointRegistry == null)
            {
                Debug.LogError("SpawnPointRegistry missing after scene load!");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb == null) return;

            SpawnPointRegistry.SpawnPoint spawnPoints = spawnPointRegistry.Get(areaId);
            if (spawnPoints == null) return;

            UpdateParallaxArea(areaData);

            if (areaPositions.ContainsKey(areaId))
            {
                playerRb.position = areaPositions[areaId];
            }
            else
            {
                playerRb.position = spawnPoints.playerSpawn.position;
            }

            CameraController cam = GetCameraController();
            if (cam != null)
            {
                cam.SetBorders(spawnPoints.leftBorder, spawnPoints.rightBorder);
            }

            SpawnEnemies(areaData, spawnPoints);
            CleanupDefeatedEncounters(areaId);
        };
    }

    private void UpdateParallaxArea(AreaData areaData)
    {
        ParallaxBackground backgroundManager = Object.FindFirstObjectByType<ParallaxBackground>();
        if (backgroundManager == null || areaData == null) return;

        foreach (ParallaxArea parallaxArea in backgroundManager.parallaxAreas)
        {
            if (parallaxArea.areaIndex == areaData.areaIndex)
            {
                backgroundManager.currentParallaxArea = parallaxArea;
                break;
            }
        }
    }

    private void SpawnEnemies(AreaData areaData, SpawnPointRegistry.SpawnPoint spawnPoints)
    {
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }

        foreach (EnemyEntry enemyEntry in areaData.enemies)
        {
            if (enemyEntry == null) continue;

            if (defeatedEncounters.ContainsKey(areaData.areaId) &&
                defeatedEncounters[areaData.areaId].Contains(enemyEntry.spawnPointId))
            {
                Debug.Log($"Skipping defeated enemy {enemyEntry.spawnPointId} in area {areaData.areaId}");
                continue;
            }

            SpawnPointRegistry.EnemySpawn enemySpawn = spawnPoints.enemySpawns
                .Find(e => e.spawnPointId == enemyEntry.spawnPointId);

            if (enemySpawn == null || enemyEntry.enemyData == null)
            {
                continue;
            }

            GameObject enemy = Instantiate(
                enemyEntry.enemyData.characterPrefab,
                enemySpawn.transform.position,
                Quaternion.identity);

            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.Initialize(enemyEntry.enemyData);
            }
        }
    }

    private void CleanupDefeatedEncounters(string areaId)
    {
        if (!defeatedEncounters.ContainsKey(areaId)) return;

        EncounterTrigger[] triggers = Object.FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None);

        foreach (EncounterTrigger trigger in triggers)
        {
            if (trigger.areaId == areaId &&
                defeatedEncounters[areaId].Contains(trigger.encounterId))
            {
                Debug.Log($"Destroying defeated encounter trigger: {trigger.encounterId}");
                Destroy(trigger.gameObject);
            }
        }
    }

    public void MarkEncounterDefeated(string areaId, string encounterId)
    {
        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(encounterId)) return;

        if (!defeatedEncounters.ContainsKey(areaId))
        {
            defeatedEncounters[areaId] = new HashSet<string>();
        }

        defeatedEncounters[areaId].Add(encounterId);
        SaveDefeatedEncounters();
        Debug.Log($"Encounter {encounterId} in area {areaId} marked as defeated.");
    }

    private void LoadDefeatedEncounters()
    {
        string defeatedString = PlayerPrefs.GetString("DefeatedEncounters", "");
        if (string.IsNullOrEmpty(defeatedString)) return;

        string[] entries = defeatedString.Split(';');
        foreach (string entry in entries)
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2) continue;

            string areaId = parts[0];
            string encounterIds = parts[1];

            if (!defeatedEncounters.ContainsKey(areaId))
            {
                defeatedEncounters[areaId] = new HashSet<string>();
            }

            string[] ids = encounterIds.Split(',');
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    defeatedEncounters[areaId].Add(id);
                }
            }
        }
    }

    private void SaveDefeatedEncounters()
    {
        List<string> entries = new();

        foreach (KeyValuePair<string, HashSet<string>> kvp in defeatedEncounters)
        {
            string areaId = kvp.Key;
            string encounterIds = string.Join(",", kvp.Value);
            entries.Add($"{areaId}:{encounterIds}");
        }

        string defeatedString = string.Join(";", entries);
        PlayerPrefs.SetString("DefeatedEncounters", defeatedString);
        PlayerPrefs.Save();
    }

    public AreaData GetCurrentAreaData()
    {
        Scene sceneToLoad = scenes.Find(s => s.sceneId == currentSceneId);
        if (sceneToLoad == null) return null;

        AreaData areaData = sceneToLoad.areas.Find(a => a.areaId == currentAreaId);
        if (areaData == null) return null;

        Debug.Log($"Current spawn area data: {areaData.areaId}");
        return areaData;
    }

    public AreaData GetAreaDataById(string areaId)
    {
        if (string.IsNullOrEmpty(areaId) || scenes == null) return null;

        foreach (Scene scene in scenes)
        {
            if (scene == null || scene.areas == null) continue;

            foreach (AreaData area in scene.areas)
            {
                if (area != null && area.areaId == areaId)
                {
                    return area;
                }
            }
        }

        return null;
    }
}
