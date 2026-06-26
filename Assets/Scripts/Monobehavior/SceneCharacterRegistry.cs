using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneCharacterRegistry : MonoBehaviour
{
    public static SceneCharacterRegistry Instance { get; private set; }

    public List<SceneCharacterEntry> characters = new();

    private readonly Dictionary<string, Transform> lookup = new();

    private void Awake()
    {
        Instance = this;
        RebuildLookup();
    }

    private void Start()
    {
        StartCoroutine(RefreshAfterSaveLoaded());
    }

    private IEnumerator RefreshAfterSaveLoaded()
    {
        // Wait until GameSaveManager has loaded cinematic save data.
        while (GameSaveManager.Instance != null && !GameSaveManager.Instance.HasLoaded)
        {
            yield return null;
        }

        ApplySavedCinematicStates();
        RebuildLookup();
    }

    public void RefreshAfterCinematicStateChanged()
    {
        ApplySavedCinematicStates();
        RebuildLookup();
    }

    public void RebuildLookup()
    {
        lookup.Clear();

        foreach (SceneCharacterEntry entry in characters)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.actorId)) continue;
            if (entry.characterTransform == null) continue;

            // Do not register hidden/destroyed cinematic actors.
            if (!entry.characterTransform.gameObject.activeInHierarchy)
            {
                continue;
            }

            RegisterActor(entry.actorId, entry.characterTransform);
        }

        AutoRegisterPlayer();

        Debug.Log($"SceneCharacterRegistry rebuilt. Registered actors: {string.Join(", ", lookup.Keys)}");
    }

    public void ApplySavedCinematicStates()
    {
        foreach (SceneCharacterEntry entry in characters)
        {
            if (entry == null) continue;
            if (entry.characterTransform == null) continue;

            if (!ShouldHideCharacter(entry))
            {
                continue;
            }

            GameObject characterObject = entry.characterTransform.gameObject;

            if (entry.destroyWhenCinematicActivated)
            {
                Debug.Log($"Destroying cinematic actor '{entry.actorId}' because cinematic was already activated.");

                Destroy(characterObject);
                entry.characterTransform = null;
            }
            else if (entry.disableWhenCinematicActivated)
            {
                Debug.Log($"Disabling cinematic actor '{entry.actorId}' because cinematic was already activated.");

                characterObject.SetActive(false);
            }
        }
    }

    private bool ShouldHideCharacter(SceneCharacterEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (entry.hideIfCinematicActivated == null)
        {
            return false;
        }

        if (FilmSceneManager.Instance == null)
        {
            return entry.hideIfCinematicActivated.isActivated;
        }

        return FilmSceneManager.Instance.IsCinematicActivated(entry.hideIfCinematicActivated);
    }

    private void AutoRegisterPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            RegisterActor("Player", playerObject.transform);
        }
    }

    public void RegisterActor(string actorId, Transform actorTransform)
    {
        if (string.IsNullOrWhiteSpace(actorId)) return;
        if (actorTransform == null) return;

        string normalizedId = NormalizeId(actorId);
        lookup[normalizedId] = actorTransform;

        Debug.Log($"Registered actor ID '{normalizedId}' -> {actorTransform.name}");
    }

    public Transform GetActor(string actorId)
    {
        return GetActor(actorId, true);
    }

    public Transform GetActor(string actorId, bool logWarnings)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            if (logWarnings)
            {
                Debug.LogWarning("Tried to get actor with empty actorId.");
            }

            return null;
        }

        string normalizedId = NormalizeId(actorId);

        if (lookup.TryGetValue(normalizedId, out Transform actor) && actor != null)
        {
            return actor;
        }

        // Last chance: maybe the player appeared after the lookup was built.
        if (normalizedId == "player")
        {
            AutoRegisterPlayer();

            if (lookup.TryGetValue(normalizedId, out actor) && actor != null)
            {
                return actor;
            }
        }

        if (logWarnings)
        {
            Debug.LogWarning($"Could not find actor with ID '{actorId}'. Normalized ID was '{normalizedId}'.");
            Debug.LogWarning($"Currently registered actors: {string.Join(", ", lookup.Keys)}");
        }

        return null;
    }

    private string NormalizeId(string actorId)
    {
        return actorId.Trim().ToLowerInvariant();
    }
}

[System.Serializable]
public class SceneCharacterEntry
{
    public string actorId;
    public Transform characterTransform;

    [Header("Cinematic Persistence")]
    public SceneData hideIfCinematicActivated;
    public bool disableWhenCinematicActivated = true;
    public bool destroyWhenCinematicActivated = false;
}