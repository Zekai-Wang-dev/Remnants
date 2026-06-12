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
        // Do it again in Start because some persistent objects may be ready after Awake.
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

            RegisterActor(entry.actorId, entry.characterTransform);
        }

        AutoRegisterPlayer();

        Debug.Log($"SceneCharacterRegistry rebuilt. Registered actors: {string.Join(", ", lookup.Keys)}");
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
        if (string.IsNullOrWhiteSpace(actorId))
        {
            Debug.LogWarning("Tried to get actor with empty actorId.");
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

        Debug.LogWarning($"Could not find actor with ID '{actorId}'. Normalized ID was '{normalizedId}'.");
        Debug.LogWarning($"Currently registered actors: {string.Join(", ", lookup.Keys)}");

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
}