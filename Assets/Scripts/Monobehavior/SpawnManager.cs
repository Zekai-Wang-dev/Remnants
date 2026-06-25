using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
    private readonly Dictionary<string, HashSet<string>> claimedInteractables = new();

    public string currentEncounterAreaId;
    public string currentEncounterId;

    private readonly List<CharacterData> pendingCustomCombatEnemies = new();

    [Header("Encounter Cooldown")]
    [SerializeField] private float postCombatRandomEncounterCooldown = 3f;

    private float randomEncounterCooldownEndTime = 0f;
    private bool isSceneTransitioning = false;
    private bool startCooldownAfterSceneLoad = false;

    private static SpawnManager _instance;
    public static SpawnManager Instance => _instance;

    public bool IsSceneTransitioning => isSceneTransitioning;

    public bool IsCurrentSceneCombat
    {
        get
        {
            if (string.Equals(currentSceneId, "Combat", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Scene currentScene = scenes != null
                ? scenes.Find(s => s != null && s.sceneId == currentSceneId)
                : null;

            return currentScene != null &&
                   string.Equals(currentScene.sceneName, "Combat", StringComparison.OrdinalIgnoreCase);
        }
    }

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
        LoadClaimedInteractables();
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
        pendingCustomCombatEnemies.Clear();
    }

    public bool StartCombatFromEnemyPool(List<CharacterData> enemies, string encounterId = "", string encounterAreaId = "")
    {
        if (isSceneTransitioning)
        {
            Debug.LogWarning("Cannot start custom combat because a scene transition is already happening.");
            return false;
        }

        pendingCustomCombatEnemies.Clear();

        if (enemies != null)
        {
            foreach (CharacterData enemy in enemies)
            {
                if (enemy != null)
                {
                    pendingCustomCombatEnemies.Add(enemy);
                }
            }
        }

        if (pendingCustomCombatEnemies.Count == 0)
        {
            Debug.LogWarning("Cannot start custom combat because no valid enemies were assigned.");
            return false;
        }

        currentEncounterId = !string.IsNullOrWhiteSpace(encounterId)
            ? encounterId.Trim()
            : $"custom_{Time.frameCount}";

        // Custom combat uses the pending enemy pool directly, so it does not need an AreaData lookup.
        // Leaving this empty also prevents custom interactable/cinematic fights from being saved as defeated fixed encounters.
        currentEncounterAreaId = !string.IsNullOrWhiteSpace(encounterAreaId)
            ? encounterAreaId.Trim()
            : "";

        NextScene("Combat");
        return true;
    }

    public bool HasPendingCustomCombatEnemyPool()
    {
        return pendingCustomCombatEnemies.Count > 0;
    }

    public List<CharacterData> ConsumePendingCustomCombatEnemyPool()
    {
        List<CharacterData> result = new();

        foreach (CharacterData enemy in pendingCustomCombatEnemies)
        {
            if (enemy != null)
            {
                result.Add(enemy);
            }
        }

        pendingCustomCombatEnemies.Clear();

        return result;
    }

    public bool IsRandomEncounterCooldownActive()
    {
        return Time.time < randomEncounterCooldownEndTime;
    }

    public float GetRandomEncounterCooldownRemaining()
    {
        return Mathf.Max(0f, randomEncounterCooldownEndTime - Time.time);
    }

    public bool CanStartRandomEncounter()
    {
        if (isSceneTransitioning)
        {
            return false;
        }

        if (IsRandomEncounterCooldownActive())
        {
            return false;
        }

        return true;
    }

    public void StartPostCombatRandomEncounterCooldown()
    {
        randomEncounterCooldownEndTime = Time.time + postCombatRandomEncounterCooldown;
        Debug.Log($"Random encounters disabled for {postCombatRandomEncounterCooldown} seconds after combat.");
    }

    private bool IsCurrentEncounterRandom()
    {
        return !string.IsNullOrEmpty(currentEncounterId) &&
               currentEncounterId.StartsWith("random_");
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
        if (isSceneTransitioning)
        {
            Debug.LogWarning("Blocked area transition because a scene transition is already happening.");
            return;
        }

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
        if (direction == "Combat")
        {
            if (isSceneTransitioning)
            {
                Debug.LogWarning("Blocked combat scene transition because a scene transition is already happening.");
                return;
            }

            if (IsCurrentEncounterRandom() && !CanStartRandomEncounter())
            {
                Debug.LogWarning($"Blocked random encounter because cooldown is active. Remaining: {GetRandomEncounterCooldownRemaining():0.00}s");
                return;
            }
        }
        else if (isSceneTransitioning)
        {
            Debug.LogWarning("Blocked scene transition because a scene transition is already happening.");
            return;
        }

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

                // Cooldown starts after the original area finishes loading.
                startCooldownAfterSceneLoad = true;
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
        if (sr != null)
        {
            sr.enabled = visible;
        }

        // Do NOT disable the player's Collider2D here.
        // Interactable OverlapCircle checks can still work without it,
        // but borders and cinematic triggers need the player collider enabled.
        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = true;
        }
    }

    private void ForceEnablePlayerPhysics()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = true;
        }
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

        ForceEnablePlayerPhysics();

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
        SpawnInteractables(areaData, spawnPoints);

        CleanupDefeatedEncounters(areaId);
        CleanupClaimedInteractables(areaId);

        QuestDataManager.Instance?.ReportLocationVisited(sceneId, areaId);
        FilmSceneManager.Instance?.RequestDelayedCinematicWorldRefresh();
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

        isSceneTransitioning = true;

        SceneManager.LoadSceneAsync(sceneToLoad.sceneName).completed += (_) =>
        {
            spawnPointRegistry = Object.FindFirstObjectByType<SpawnPointRegistry>();
            if (spawnPointRegistry == null)
            {
                Debug.LogError("SpawnPointRegistry missing after scene load!");
                isSceneTransitioning = false;
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                isSceneTransitioning = false;
                return;
            }

            ForceEnablePlayerPhysics();

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb == null)
            {
                isSceneTransitioning = false;
                return;
            }

            SpawnPointRegistry.SpawnPoint spawnPoints = spawnPointRegistry.Get(areaId);
            if (spawnPoints == null)
            {
                isSceneTransitioning = false;
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
            SpawnInteractables(areaData, spawnPoints);

            CleanupDefeatedEncounters(areaId);
            CleanupClaimedInteractables(areaId);

            QuestDataManager.Instance?.ReportLocationVisited(sceneId, areaId);

            isSceneTransitioning = false;

            if (startCooldownAfterSceneLoad)
            {
                startCooldownAfterSceneLoad = false;
                StartPostCombatRandomEncounterCooldown();
            }

            FilmSceneManager.Instance?.RequestDelayedCinematicWorldRefresh();
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

    private void SpawnInteractables(AreaData areaData, SpawnPointRegistry.SpawnPoint spawnPoints)
    {
        foreach (GameObject interactable in GameObject.FindGameObjectsWithTag("Interactable"))
        {
            Destroy(interactable);
        }

        if (areaData == null || areaData.interactables == null)
        {
            return;
        }

        if (spawnPoints == null || spawnPoints.interactableSpawns == null)
        {
            return;
        }

        foreach (InteractableEntry interactableEntry in areaData.interactables)
        {
            if (interactableEntry == null)
            {
                continue;
            }

            if (interactableEntry.interactableData == null)
            {
                Debug.LogWarning($"Interactable entry in area {areaData.areaId} has no InteractableData.");
                continue;
            }

            string interactableSaveId = GetInteractableSaveId(interactableEntry);

            if (string.IsNullOrEmpty(interactableSaveId))
            {
                Debug.LogWarning($"Interactable entry in area {areaData.areaId} has no valid save ID.");
                continue;
            }

            bool canRefresh = CanInteractableRefresh(interactableEntry);

            if (canRefresh)
            {
                RemoveClaimedInteractable(areaData.areaId, interactableSaveId, false);
            }
            else if (IsInteractableClaimed(areaData.areaId, interactableSaveId))
            {
                Debug.Log($"Skipping claimed interactable {interactableSaveId} in area {areaData.areaId}");
                continue;
            }

            SpawnPointRegistry.InteractableSpawn interactableSpawn = spawnPoints.interactableSpawns
                .Find(i => i.spawnPointId == interactableEntry.spawnPointId);

            if (interactableSpawn == null || interactableSpawn.transform == null)
            {
                Debug.LogWarning($"No interactable spawn point found for ID {interactableEntry.spawnPointId} in area {areaData.areaId}");
                continue;
            }

            if (interactableEntry.interactableData.interactablePrefab == null)
            {
                Debug.LogWarning($"InteractableData {interactableEntry.interactableData.name} has no prefab.");
                continue;
            }

            GameObject interactableObject = Instantiate(
                interactableEntry.interactableData.interactablePrefab,
                interactableSpawn.transform.position,
                Quaternion.identity);

            WorldInteractable worldInteractable = interactableObject.GetComponent<WorldInteractable>();

            if (worldInteractable != null)
            {
                worldInteractable.Initialize(
                    interactableEntry.interactableData,
                    areaData.areaId,
                    interactableSaveId);
            }
            else
            {
                Debug.LogWarning($"Spawned interactable {interactableObject.name} has no WorldInteractable component.");
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

    private void CleanupClaimedInteractables(string areaId)
    {
        if (string.IsNullOrEmpty(areaId)) return;
        if (!claimedInteractables.ContainsKey(areaId)) return;

        WorldInteractable[] interactables = Object.FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None);

        foreach (WorldInteractable interactable in interactables)
        {
            if (interactable == null) continue;

            string interactableSaveId = interactable.GetInteractableSaveId();

            if (string.IsNullOrEmpty(interactableSaveId))
            {
                continue;
            }

            if (!claimedInteractables[areaId].Contains(interactableSaveId))
            {
                continue;
            }

            if (IsRefreshableInteractable(areaId, interactableSaveId))
            {
                RemoveClaimedInteractable(areaId, interactableSaveId, false);
                Debug.Log($"Keeping refreshable interactable: {interactableSaveId}");
                continue;
            }

            Debug.Log($"Destroying claimed interactable: {interactableSaveId}");
            Destroy(interactable.gameObject);
        }

        SaveClaimedInteractables();
    }

    private bool CanInteractableRefresh(InteractableEntry interactableEntry)
    {
        return interactableEntry != null &&
               interactableEntry.interactableData != null &&
               interactableEntry.interactableData.canRefresh;
    }

    private bool IsRefreshableInteractable(string areaId, string interactableId)
    {
        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(interactableId))
        {
            return false;
        }

        AreaData areaData = GetAreaDataById(areaId);

        if (areaData == null || areaData.interactables == null)
        {
            return false;
        }

        foreach (InteractableEntry interactableEntry in areaData.interactables)
        {
            if (interactableEntry == null)
            {
                continue;
            }

            string saveId = GetInteractableSaveId(interactableEntry);

            if (saveId == interactableId)
            {
                return CanInteractableRefresh(interactableEntry);
            }
        }

        return false;
    }

    private void RemoveClaimedInteractable(string areaId, string interactableId, bool saveAfterRemove)
    {
        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(interactableId))
        {
            return;
        }

        if (!claimedInteractables.ContainsKey(areaId))
        {
            return;
        }

        bool removed = claimedInteractables[areaId].Remove(interactableId);

        if (claimedInteractables[areaId].Count <= 0)
        {
            claimedInteractables.Remove(areaId);
        }

        if (removed && saveAfterRemove)
        {
            SaveClaimedInteractables();
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

    private string GetInteractableSaveId(InteractableEntry interactableEntry)
    {
        if (interactableEntry == null) return "";

        if (!string.IsNullOrEmpty(interactableEntry.spawnPointId))
        {
            return interactableEntry.spawnPointId;
        }

        if (interactableEntry.interactableData != null &&
            !string.IsNullOrEmpty(interactableEntry.interactableData.interactableId))
        {
            return interactableEntry.interactableData.interactableId;
        }

        return "";
    }

    public bool IsInteractableClaimed(string areaId, string interactableId)
    {
        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(interactableId))
        {
            return false;
        }

        return claimedInteractables.ContainsKey(areaId) &&
               claimedInteractables[areaId].Contains(interactableId);
    }

    public void MarkInteractableClaimed(string areaId, string interactableId)
    {
        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(interactableId))
        {
            return;
        }

        if (IsRefreshableInteractable(areaId, interactableId))
        {
            RemoveClaimedInteractable(areaId, interactableId, true);
            Debug.Log($"Interactable {interactableId} in area {areaId} can refresh, so it was not permanently marked as claimed.");
            return;
        }

        if (!claimedInteractables.ContainsKey(areaId))
        {
            claimedInteractables[areaId] = new HashSet<string>();
        }

        claimedInteractables[areaId].Add(interactableId);
        SaveClaimedInteractables();

        Debug.Log($"Interactable {interactableId} in area {areaId} marked as claimed.");
    }

    private void LoadClaimedInteractables()
    {
        string claimedString = PlayerPrefs.GetString("ClaimedInteractables", "");
        if (string.IsNullOrEmpty(claimedString)) return;

        string[] entries = claimedString.Split(';');

        foreach (string entry in entries)
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2) continue;

            string areaId = parts[0];
            string interactableIds = parts[1];

            if (!claimedInteractables.ContainsKey(areaId))
            {
                claimedInteractables[areaId] = new HashSet<string>();
            }

            string[] ids = interactableIds.Split(',');

            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    claimedInteractables[areaId].Add(id);
                }
            }
        }
    }

    private void SaveClaimedInteractables()
    {
        List<string> entries = new();

        foreach (KeyValuePair<string, HashSet<string>> kvp in claimedInteractables)
        {
            string areaId = kvp.Key;
            string interactableIds = string.Join(",", kvp.Value);
            entries.Add($"{areaId}:{interactableIds}");
        }

        string claimedString = string.Join(";", entries);
        PlayerPrefs.SetString("ClaimedInteractables", claimedString);
        PlayerPrefs.Save();
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

    public void ClearRuntimeSavedState()
    {
        defeatedEncounters.Clear();
        claimedInteractables.Clear();

        currentEncounterAreaId = "";
        currentEncounterId = "";
        pendingCustomCombatEnemies.Clear();

        areaPositions.Clear();

        Debug.Log("SpawnManager runtime saved state cleared.");
    }
}