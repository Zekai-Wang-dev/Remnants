#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameIdValidatorWindow : EditorWindow
{
    private Vector2 scroll;
    private readonly List<string> messages = new List<string>();

    [MenuItem("Tools/Remnants/ID Validator")]
    public static void Open()
    {
        GetWindow<GameIdValidatorWindow>("ID Validator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Remnants ID Validator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this after importing the fixed scripts. It auto-fills missing IDs from asset names, normalizes typed IDs, rebuilds SceneData required actor IDs, and reports duplicates.",
            MessageType.Info);

        if (GUILayout.Button("Auto-Fix IDs + Validate"))
        {
            AutoFixAndValidate();
        }

        EditorGUILayout.Space(8);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string message in messages)
        {
            EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    private void AutoFixAndValidate()
    {
        messages.Clear();

        ValidateAssets<AreaData>("AreaData", GameIdUtility.GetAreaId, asset =>
        {
            asset.areaId = GameIdUtility.CleanOrCreate(asset.areaId, asset);
        });

        ValidateAssets<CharacterData>("CharacterData", GameIdUtility.GetCharacterId, asset =>
        {
            asset.characterId = GameIdUtility.CleanOrCreate(asset.characterId, asset);
        });

        ValidateAssets<InteractableData>("InteractableData", GameIdUtility.GetInteractableId, asset =>
        {
            asset.interactableId = GameIdUtility.CleanOrCreate(asset.interactableId, asset);
        });

        ValidateAssets<ItemData>("ItemData", GameIdUtility.GetItemId, asset =>
        {
            asset.itemId = GameIdUtility.CleanOrCreate(asset.itemId, asset);
        });

        ValidateAssets<QuestData>("QuestData", GameIdUtility.GetQuestId, asset =>
        {
            asset.questId = GameIdUtility.CleanOrCreate(asset.questId, asset);
        });

        ValidateAssets<SceneData>("SceneData", GameIdUtility.GetSceneId, asset =>
        {
            asset.sceneId = GameIdUtility.CleanOrCreate(asset.sceneId, asset);
            asset.requiredSceneId = GameIdUtility.Normalize(asset.requiredSceneId);
            asset.requiredAreaId = GameIdUtility.Normalize(asset.requiredAreaId);
            asset.RebuildRequiredActorIds();
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (messages.Count == 0)
        {
            messages.Add("No problems found.");
        }
    }

    private void ValidateAssets<T>(string label, System.Func<T, string> getId, System.Action<T> autoFix) where T : Object
    {
        Dictionary<string, List<string>> pathsById = new Dictionary<string, List<string>>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                continue;
            }

            autoFix?.Invoke(asset);
            EditorUtility.SetDirty(asset);

            string id = getId(asset);

            if (string.IsNullOrWhiteSpace(id))
            {
                messages.Add($"[{label}] Missing ID: {path}");
                continue;
            }

            if (!pathsById.ContainsKey(id))
            {
                pathsById[id] = new List<string>();
            }

            pathsById[id].Add(path);
        }

        foreach (KeyValuePair<string, List<string>> pair in pathsById)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }

            messages.Add($"[{label}] Duplicate ID '{pair.Key}':");
            foreach (string path in pair.Value)
            {
                messages.Add($"  - {path}");
            }
        }
    }
}
#endif
