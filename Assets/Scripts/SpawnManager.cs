using JetBrains.Annotations;
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
    
}

[System.Serializable]
public class Scene
{

    public string sceneName; // Name of the scene to load
    public string sceneId;
    public Vector2 sceneIndex;
    public List<Area> areas; 

}

public class SpawnManager : MonoBehaviour
{

    public List<Scene> scenes; // List of scenes with their areas
    public string currentSceneId; // ID of the current scene
    public string currentAreaId;
    public Vector2 currentAreaIndex;
    public Vector2 currentSceneIndex; 

    private static SpawnManager _instance;
    public static SpawnManager Instance => _instance;
    public void Awake()
    {

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Ensure only one instance of SpawnManager exists
            return;
        }

        // Load the initial scene and area
        LoadScene(currentSceneId, currentAreaId);
        DontDestroyOnLoad(gameObject);
        _instance = this;
    }

    public int FindSceneIndex(string currentSceneId)
    {

        int scenesIndex = 0;

        for (int i = 0; i < scenes.Count; i++ )
        {

            if (scenes[i].sceneId == currentSceneId)
            {
                scenesIndex = i;
                break;
            }

        }

        return scenesIndex;
    }

    public int FindAreaIndex(string currentAreaId)
    {
        int areaIndex = 0;
        for (int i = 0; i < scenes[FindSceneIndex(currentSceneId)].areas.Count; i++)
        {
            if (scenes[FindSceneIndex(currentSceneId)].areas[i].areaId == currentAreaId)
            {
                areaIndex = i;
                break;
            }
        }
        return areaIndex;
    }

    public void UpdateCurrentIdInfo()
    {

        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneIndex == currentSceneIndex)
            {
                currentSceneId = scenes[i].sceneId;

            }

        }

        for (int i = 0; i < scenes[FindSceneIndex(currentSceneId)].areas.Count; i++)
        {
            if (scenes[FindSceneIndex(currentSceneId)].areas[i].areaIndex == currentAreaIndex)
            {
                currentAreaId = scenes[FindSceneIndex(currentSceneId)].areas[i].areaId;
            }
        }

    }

    public void NextArea(string Direction)
    {

        if (Direction == "Left")
        {
            currentAreaIndex.x -= 1;
            UpdateCurrentIdInfo();
            LoadArea(currentSceneId, currentAreaId);

        }
        else if (Direction == "Right")
        {
            currentAreaIndex.x += 1;
            UpdateCurrentIdInfo();
            LoadArea(currentSceneId, currentAreaId);

        }
        else if (Direction == "Forward")
        {
            currentAreaIndex.y += 1;
            UpdateCurrentIdInfo();
            LoadArea(currentSceneId, currentAreaId);

        }
        else if (Direction == "Backward")
        {
            currentAreaIndex.y -= 1;
            UpdateCurrentIdInfo();
            LoadArea(currentSceneId, currentAreaId);

        }

    }

    public void NextScene(string Direction)
    {
        if (Direction == "Left")
        {
            currentSceneIndex.x -= 1;
            UpdateCurrentIdInfo();
            LoadScene(currentSceneId, currentAreaId);
        }
        else if (Direction == "Right")
        {
            currentSceneIndex.x += 1;
            UpdateCurrentIdInfo();
            LoadScene(currentSceneId, currentAreaId);
        }
        else if (Direction == "Forward")
        {
            currentSceneIndex.y += 1;
            UpdateCurrentIdInfo();
            LoadScene(currentSceneId, currentAreaId);
        }
        else if (Direction == "Backward")
        {
            currentSceneIndex.y -= 1;
            UpdateCurrentIdInfo();
            LoadScene(currentSceneId, currentAreaId);
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
                // After the scene is loaded, set the player's position to the spawn area
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = areaToSpawn.spawnArea;
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
                // Load the scene asynchronously
                SceneManager.LoadSceneAsync(sceneToLoad.sceneName).completed += (asyncOperation) =>
                {
                    // After the scene is loaded, set the player's position to the spawn area
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        player.transform.position = areaToSpawn.spawnArea;
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