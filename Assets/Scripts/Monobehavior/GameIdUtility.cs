using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class GameIdUtility
{
    public const string RandomEncounterPrefix = "random_";

    public static string Normalize(string value, bool stripRandomPrefix = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (stripRandomPrefix && normalized.StartsWith(RandomEncounterPrefix))
        {
            normalized = normalized.Substring(RandomEncounterPrefix.Length);
        }

        return normalized;
    }

    public static string ToStableId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        value = value.Trim().ToLowerInvariant();
        StringBuilder builder = new StringBuilder(value.Length);
        bool previousWasSeparator = false;

        foreach (char c in value)
        {
            bool valid = (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9');

            if (valid)
            {
                builder.Append(c);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('_');
    }

    public static string CleanOrCreate(string currentValue, Object sourceObject, string prefix = "")
    {
        string source = !string.IsNullOrWhiteSpace(currentValue)
            ? currentValue
            : sourceObject != null
                ? sourceObject.name
                : "";

        string id = ToStableId(source);
        string normalizedPrefix = ToStableId(prefix);

        if (!string.IsNullOrWhiteSpace(normalizedPrefix) && !string.IsNullOrWhiteSpace(id) && !id.StartsWith(normalizedPrefix + "_"))
        {
            id = normalizedPrefix + "_" + id;
        }

        return id;
    }

    public static bool IdEquals(string a, string b, bool stripRandomPrefix = false)
    {
        return Normalize(a, stripRandomPrefix) == Normalize(b, stripRandomPrefix);
    }

    public static string Build(params string[] parts)
    {
        List<string> cleanParts = new List<string>();

        if (parts != null)
        {
            foreach (string part in parts)
            {
                string clean = ToStableId(part);
                if (!string.IsNullOrWhiteSpace(clean))
                {
                    cleanParts.Add(clean);
                }
            }
        }

        return string.Join("_", cleanParts);
    }

    public static string GetAreaId(AreaData areaData)
    {
        if (areaData == null) return "";
        return !string.IsNullOrWhiteSpace(areaData.areaId)
            ? Normalize(areaData.areaId)
            : ToStableId(areaData.name);
    }

    public static string GetCharacterId(CharacterData characterData)
    {
        if (characterData == null) return "";
        return !string.IsNullOrWhiteSpace(characterData.characterId)
            ? Normalize(characterData.characterId)
            : ToStableId(characterData.name);
    }

    public static string GetInteractableId(InteractableData interactableData)
    {
        if (interactableData == null) return "";
        return !string.IsNullOrWhiteSpace(interactableData.interactableId)
            ? Normalize(interactableData.interactableId)
            : ToStableId(interactableData.name);
    }

    public static string GetItemId(ItemData itemData)
    {
        if (itemData == null) return "";
        return !string.IsNullOrWhiteSpace(itemData.itemId)
            ? Normalize(itemData.itemId)
            : ToStableId(itemData.name);
    }

    public static string GetQuestId(QuestData questData)
    {
        if (questData == null) return "";
        return !string.IsNullOrWhiteSpace(questData.questId)
            ? Normalize(questData.questId)
            : ToStableId(questData.name);
    }

    public static string GetSceneId(SceneData sceneData)
    {
        if (sceneData == null) return "";
        return !string.IsNullOrWhiteSpace(sceneData.sceneId)
            ? Normalize(sceneData.sceneId)
            : ToStableId(sceneData.name);
    }
}
