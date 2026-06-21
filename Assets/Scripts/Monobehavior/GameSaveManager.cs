using System;
using System.Collections;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public InventorySaveData inventory;
    public QuestSaveData quests;
}

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Databases")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Save Settings")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool saveOnQuit = true;
    [SerializeField] private bool saveOnPause = true;
    [SerializeField] private bool saveOnFocusLost = true;
    [SerializeField] private bool autosaveEnabled = true;
    [SerializeField] private float autosaveInterval = 5f;

    [Header("Reset Settings")]
    [SerializeField] private string resetSceneId = "002";
    [SerializeField] private string resetAreaId = "001";

    private bool hasLoaded;
    private bool isQuitting;
    private Coroutine autosaveCoroutine;

    public bool HasLoaded => hasLoaded;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    // PlayerPrefs save keys used by other systems.
    private const string DefeatedEncountersKey = "DefeatedEncounters";
    private const string ClaimedInteractablesKey = "ClaimedInteractables";
    private const string CollectedOnceOnlyDropsKey = "CollectedOnceOnlyDrops";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (itemDatabase != null)
        {
            itemDatabase.Initialize();
        }

        if (questDatabase != null)
        {
            questDatabase.Initialize();
        }
    }

    private IEnumerator Start()
    {
        // Wait one frame so InventoryManager, QuestDataManager, PlayerDataManager,
        // SpawnManager, etc. can finish Awake().
        yield return null;

        if (loadOnStart)
        {
            LoadGame();
        }
        else
        {
            hasLoaded = true;
        }

        if (autosaveEnabled)
        {
            autosaveCoroutine = StartCoroutine(AutoSaveLoop());
        }
    }

    private IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(autosaveInterval);

            if (hasLoaded)
            {
                SaveGame();
            }
        }
    }

    public void RequestSave()
    {
        if (!hasLoaded)
        {
            return;
        }

        SaveGame();
    }

    public void SaveGame()
    {
        GameSaveData saveData = new GameSaveData();

        if (InventoryManager.Instance != null)
        {
            saveData.inventory = InventoryManager.Instance.CreateSaveData();
        }

        if (QuestDataManager.Instance != null)
        {
            saveData.quests = QuestDataManager.Instance.CreateSaveData();
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"Game saved to: {SavePath}");
    }

    public void LoadGame()
    {
        if (itemDatabase != null)
        {
            itemDatabase.Initialize();
        }

        if (questDatabase != null)
        {
            questDatabase.Initialize();
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log($"No save file found at: {SavePath}. Starting new save.");
            hasLoaded = true;
            return;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        if (saveData == null)
        {
            Debug.LogWarning("Save file could not be parsed.");
            hasLoaded = true;
            return;
        }

        if (InventoryManager.Instance != null && saveData.inventory != null)
        {
            if (itemDatabase == null)
            {
                Debug.LogWarning("Cannot load inventory because itemDatabase is not assigned.");
            }
            else
            {
                InventoryManager.Instance.LoadFromSaveData(saveData.inventory, itemDatabase);
            }
        }

        if (QuestDataManager.Instance != null && saveData.quests != null)
        {
            if (questDatabase == null)
            {
                Debug.LogWarning("Cannot load quests because questDatabase is not assigned.");
            }
            else
            {
                QuestDataManager.Instance.LoadFromSaveData(saveData.quests, questDatabase);
            }
        }

        hasLoaded = true;
        Debug.Log("Game loaded.");
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log($"Deleted save file at: {SavePath}");
        }
    }

    public void DeleteAllSavedData()
    {
        // Prevent autosave / quit-save from immediately recreating save.json.
        hasLoaded = false;

        if (autosaveCoroutine != null)
        {
            StopCoroutine(autosaveCoroutine);
            autosaveCoroutine = null;
        }

        DeleteSaveFile();

        PlayerPrefs.DeleteKey(DefeatedEncountersKey);
        PlayerPrefs.DeleteKey(ClaimedInteractablesKey);
        PlayerPrefs.DeleteKey(CollectedOnceOnlyDropsKey);
        PlayerPrefs.Save();

        if (QuestDataManager.Instance != null)
        {
            QuestDataManager.Instance.ClearRuntimeQuestData();
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
        }

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ClearPlayerData(true);
        }

        StoryFlagManager.ClearFlags();

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ClearRuntimeSavedState();

            SpawnManager.Instance.currentSceneId = resetSceneId;
            SpawnManager.Instance.currentAreaId = resetAreaId;

            SpawnManager.Instance.LoadScene(resetSceneId, resetAreaId);
        }

        hasLoaded = true;

        if (autosaveEnabled && autosaveCoroutine == null)
        {
            autosaveCoroutine = StartCoroutine(AutoSaveLoop());
        }

        Debug.Log("Deleted all saved game data and reset runtime state.");
    }

    private void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log($"Deleted save file at: {SavePath}");
        }
        else
        {
            Debug.Log($"No save file found to delete at: {SavePath}");
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;

        if (saveOnQuit && hasLoaded)
        {
            SaveGame();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnPause)
        {
            return;
        }

        if (!hasLoaded)
        {
            return;
        }

        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!saveOnFocusLost)
        {
            return;
        }

        if (!hasLoaded)
        {
            return;
        }

        if (isQuitting)
        {
            return;
        }

        if (!hasFocus)
        {
            SaveGame();
        }
    }
}