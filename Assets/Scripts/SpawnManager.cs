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

    private Dictionary<string, Vector2> areaPositions = new Dictionary<string, Vector2>();
    private SpawnPointRegistry spawnPointRegistry;

    private static SpawnManager _instance;
    public static SpawnManager Instance => _instance;

    private PlayerController GetPlayerController()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player?.GetComponent<PlayerController>();
    }

    private CameraController GetCameraController()
    {
        return Camera.main?.GetComponent<CameraController>();
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
    }

    private void Start()
    {
        spawnPointRegistry = Object.FindFirstObjectByType<SpawnPointRegistry>();
        if (spawnPointRegistry == null)
            Debug.LogError("SpawnPointRegistry not found in scene!");

        LoadScene(currentSceneId, currentAreaId);
    }

    public int FindSceneIndex(string currentSceneId)
    {
        for (int i = 0; i < scenes.Count; i++)
            if (scenes[i].sceneId == currentSceneId)
                return i;
        return 0;
    }

    public int FindAreaIndex(string currentAreaId)
    {
        int sceneIdx = FindSceneIndex(currentSceneId);
        for (int i = 0; i < scenes[sceneIdx].areas.Count; i++)
            if (scenes[sceneIdx].areas[i].areaId == currentAreaId)
                return i;
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
        for (int i = 0; i < scenes[sceneIdx].areas.Count; i++)
        {
            if (scenes[sceneIdx].areas[i].areaIndex == currentAreaIndex)
            {
                currentAreaId = scenes[sceneIdx].areas[i].areaId;
                break;
            }
        }

        if (currentAreaId == previousAreaId)
            currentAreaIndex = previousAreaIndex;

        if (currentSceneId == previousSceneId)
            currentSceneIndex = previousSceneIndex;
    }

    public void NextArea(string direction)
    {
        StoreCurrentAreaPosition();

        previousAreaIndex = currentAreaIndex;
        previousAreaId = currentAreaId;

        switch (direction)
        {
            case "Left": currentAreaIndex.x--; break;
            case "Right": currentAreaIndex.x++; break;
            case "Forward": currentAreaIndex.y++; break;
            case "Backward": currentAreaIndex.y--; break;
        }

        UpdateCurrentIdInfo();
        LoadArea(currentSceneId, currentAreaId);
        GetCameraController()?.changeBorders();
    }

    public void NextScene(string direction)
    {
        StoreCurrentAreaPosition();

        previousSceneIndex = currentSceneIndex;
        previousSceneId = currentSceneId;

        switch (direction)
        {
            case "Left": currentSceneIndex.x--; break;
            case "Right": currentSceneIndex.x++; break;
            case "Forward": currentSceneIndex.y++; break;
            case "Backward": currentSceneIndex.y--; break;
        }

        UpdateCurrentIdInfo();
        LoadScene(currentSceneId, currentAreaId);
        GetCameraController()?.changeBorders();
    }

    private void StoreCurrentAreaPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
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

        SpawnPointRegistry.SpawnPoint spawnPoints = spawnPointRegistry.Get(areaId);
        if (spawnPoints == null)
        {
            Debug.LogError($"No spawn points found for area {areaId}");
            return;
        }

        ParallaxBackground backgroundManager = Object.FindFirstObjectByType<ParallaxBackground>();
        if (backgroundManager != null)
        {
            foreach (var parallaxArea in backgroundManager.parallaxAreas)
            {
                if (parallaxArea.areaIndex == areaData.areaIndex)
                {
                    backgroundManager.currentParallaxArea = parallaxArea;
                    break;
                }
            }
        }

        if (areaPositions.ContainsKey(areaId))
            playerRb.position = areaPositions[areaId];
        else
            playerRb.position = spawnPoints.playerSpawn.position;

        CameraController cam = GetCameraController();
        if (cam != null)
            cam.SetBorders(spawnPoints.leftBorder, spawnPoints.rightBorder);

        SpawnEnemies(areaData, spawnPoints);
    }

    public void LoadScene(string sceneId, string areaId)
    {
        Scene sceneToLoad = scenes.Find(s => s.sceneId == sceneId);
        if (sceneToLoad == null) return;

        AreaData areaData = sceneToLoad.areas.Find(a => a.areaId == areaId);
        if (areaData == null) return;

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

            if (areaPositions.ContainsKey(areaId))
                playerRb.position = areaPositions[areaId];
            else
                playerRb.position = spawnPoints.playerSpawn.position;

            CameraController cam = GetCameraController();
            if (cam != null)
                cam.SetBorders(spawnPoints.leftBorder, spawnPoints.rightBorder);

            SpawnEnemies(areaData, spawnPoints);
        };
    }

    private void SpawnEnemies(AreaData areaData, SpawnPointRegistry.SpawnPoint spawnPoints)
    {
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(enemy);

        foreach (var enemyEntry in areaData.enemies)
        {
            SpawnPointRegistry.EnemySpawn enemySpawn = spawnPoints.enemySpawns
                .Find(e => e.spawnPointId == enemyEntry.spawnPointId);

            if (enemySpawn == null || enemyEntry.enemyData == null)
                continue;

            GameObject enemy = Instantiate(
                enemyEntry.enemyData.characterPrefab,
                enemySpawn.transform.position,
                Quaternion.identity
            );

            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
                ec.Initialize(enemyEntry.enemyData);
        }
    }
}