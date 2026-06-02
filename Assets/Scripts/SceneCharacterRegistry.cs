using System.Collections.Generic;
using UnityEngine;

public class SceneCharacterRegistry : MonoBehaviour
{
    public static SceneCharacterRegistry Instance { get; private set; }

    public List<SceneCharacterEntry> characters = new();

    private Dictionary<string, Transform> lookup = new();

    private void Awake()
    {
        Instance = this;

        lookup.Clear();

        foreach (SceneCharacterEntry entry in characters)
        {
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.actorId)) continue;
            if (entry.characterTransform == null) continue;

            lookup[entry.actorId] = entry.characterTransform;
        }
    }

    public Transform GetActor(string actorId)
    {
        if (string.IsNullOrEmpty(actorId))
        {
            return null;
        }

        lookup.TryGetValue(actorId, out Transform actor);
        return actor;
    }
}

[System.Serializable]
public class SceneCharacterEntry
{
    public string actorId;
    public Transform characterTransform;
}