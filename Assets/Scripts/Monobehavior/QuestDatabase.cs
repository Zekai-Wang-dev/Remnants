using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    public List<QuestData> quests = new List<QuestData>();

    private Dictionary<string, QuestData> lookup;

    public void Initialize()
    {
        lookup = new Dictionary<string, QuestData>();

        foreach (QuestData quest in quests)
        {
            if (quest == null)
            {
                continue;
            }

            string questId = GetQuestId(quest);

            if (string.IsNullOrWhiteSpace(questId))
            {
                Debug.LogWarning($"QuestData '{quest.name}' has no questId and no usable asset name.");
                continue;
            }

            if (!lookup.ContainsKey(questId))
            {
                lookup.Add(questId, quest);
            }
            else
            {
                Debug.LogWarning($"Duplicate questId found in QuestDatabase: {questId}");
            }
        }
    }

    public QuestData GetQuestById(string questId)
    {
        if (lookup == null)
        {
            Initialize();
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        lookup.TryGetValue(NormalizeId(questId), out QuestData quest);
        return quest;
    }

    public string GetQuestId(QuestData questData)
    {
        if (questData == null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(questData.questId))
        {
            return NormalizeId(questData.questId);
        }

        return NormalizeId(questData.name);
    }

    private string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return value.Trim().ToLowerInvariant();
    }
}