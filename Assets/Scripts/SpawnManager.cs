using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Area
{
    public string areaName;
    public string areaId;
    public string areaType;
    public Vector2 areaIndex;
    public Vector2 spawnArea;
    public Vector2 leftBorder; 
    public Vector2 rightBorder;
}

[System.Serializable]
public class Scene
{
    public string sceneName;
    public string sceneId;
    public Vector2 sceneIndex;
    public List<Area> areas;
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

    // Store player positions for EACH area separately
    private Dictionary<string, Vector2> areaPositions = new Dictionary<string, Vector2>();

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

    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadScene(currentSceneId, currentAreaId);
        DontDestroyOnLoad(gameObject);
        _instance = this;
    }

    public int FindSceneIndex(string currentSceneId)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneId == currentSceneId)
                return i;
        }
        return 0;
    }

    public int FindAreaIndex(string currentAreaId)
    {
        int sceneIdx = FindSceneIndex(currentSceneId);
        for (int i = 0; i < scenes[sceneIdx].areas.Count; i++)
        {
            if (scenes[sceneIdx].areas[i].areaId == currentAreaId)
                return i;
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
        for (int i = 0; i < scenes[sceneIdx].areas.Count; i++)
        {
            if (scenes[sceneIdx].areas[i].areaIndex == currentAreaIndex)
            {

                currentAreaId = scenes[sceneIdx].areas[i].areaId;
                break;

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

    public void NextArea(string Direction)
    {
        // Store current position BEFORE changing area
        StoreCurrentAreaPosition();

        previousAreaIndex = currentAreaIndex;
        previousAreaId = currentAreaId;

        if (Direction == "Left")
            currentAreaIndex.x -= 1;
        else if (Direction == "Right")
            currentAreaIndex.x += 1;
        else if (Direction == "Forward")
            currentAreaIndex.y += 1;
        else if (Direction == "Backward")
            currentAreaIndex.y -= 1;

        UpdateCurrentIdInfo();
        LoadArea(currentSceneId, currentAreaId);
        GetCameraController()?.changeBorders();
    }

    public void NextScene(string Direction)
    {
        // Store current position BEFORE changing scene
        StoreCurrentAreaPosition();

        previousSceneIndex = currentSceneIndex; // Store the scene index 
        previousSceneId = currentSceneId;

        if (Direction == "Left")
            currentSceneIndex.x -= 1;
        else if (Direction == "Right")
            currentSceneIndex.x += 1;
        else if (Direction == "Forward")
            currentSceneIndex.y += 1;
        else if (Direction == "Backward")
            currentSceneIndex.y -= 1;

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
            {
                // Store position for THIS specific area
                areaPositions[currentAreaId] = playerRb.position;
            }
        }
    }

    public void LoadArea(string sceneId, string areaId)
    {
        Scene sceneToLoad = scenes.Find(scene => scene.sceneId == sceneId);
        if (sceneToLoad != null)
        {
            Area areaToSpawn = sceneToLoad.areas.Find(area => area.areaId == areaId);
            if (areaToSpawn != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Rigidbody2D playerRb = player?.GetComponent<Rigidbody2D>();

                if (playerRb != null)
                {
                    // Check if we have a stored position for this area
                    if (areaPositions.ContainsKey(areaId))
                    {
                        // Return to exact position where player left
                        playerRb.position = areaPositions[areaId];
                    }
                    else
                    {
                        // First time entering this area - use spawn point
                        playerRb.position = areaToSpawn.spawnArea;
                    }
                }
            }
            else
            {
                Debug.LogError($"Area with ID {areaId} not found in scene {sceneId}");
            }
        }
        else
        {
            Debug.LogError($"Scene with ID {sceneId} not found");
        }
    }

    public void LoadScene(string sceneId, string areaId)
    {
        Scene sceneToLoad = scenes.Find(scene => scene.sceneId == sceneId);
        if (sceneToLoad != null)
        {
            Area areaToSpawn = sceneToLoad.areas.Find(area => area.areaId == areaId);
            if (areaToSpawn != null)
            {
                SceneManager.LoadSceneAsync(sceneToLoad.sceneName).completed += (asyncOperation) =>
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                        if (playerRb != null)
                        {
                            // Check if we have a stored position for this area
                            if (areaPositions.ContainsKey(areaId))
                            {
                                playerRb.position = areaPositions[areaId];
                            }
                            else
                            {
                                playerRb.position = areaToSpawn.spawnArea;
                            }
                        }
                    }
                };
            }
            else
            {
                Debug.LogError($"Area with ID {areaId} not found in scene {sceneId}");
            }
        }
        else
        {
            Debug.LogError($"Scene with ID {sceneId} not found");
        }
    }
}