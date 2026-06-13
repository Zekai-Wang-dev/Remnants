using System.Collections;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public InventorySaveData inventory;
}

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Databases")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Save Settings")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool saveOnQuit = true;
    [SerializeField] private bool saveOnPause = true;
    [SerializeField] private bool saveOnFocusLost = true;
    [SerializeField] private bool autosaveEnabled = true;
    [SerializeField] private float autosaveInterval = 5f;

    private bool hasLoaded;
    private bool isQuitting;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

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
    }

    private IEnumerator Start()
    {
        // Wait one frame so InventoryManager, PlayerDataManager, SpawnManager, etc. can finish Awake().
        yield return null;

        if (loadOnStart)
        {
            LoadGame();
        }

        if (autosaveEnabled)
        {
            StartCoroutine(AutoSaveLoop());
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

    public void SaveGame()
    {
        GameSaveData saveData = new GameSaveData();

        if (InventoryManager.Instance != null)
        {
            saveData.inventory = InventoryManager.Instance.CreateSaveData();
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"Game saved to: {SavePath}");
    }

    public void LoadGame()
    {
        if (itemDatabase == null)
        {
            Debug.LogError("GameSaveManager cannot load because itemDatabase is not assigned.");
            hasLoaded = true;
            return;
        }

        itemDatabase.Initialize();

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
            InventoryManager.Instance.LoadFromSaveData(saveData.inventory, itemDatabase);
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
        if (!saveOnPause) return;
        if (!hasLoaded) return;

        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!saveOnFocusLost) return;
        if (!hasLoaded) return;
        if (isQuitting) return;

        if (!hasFocus)
        {
            SaveGame();
        }
    }
}