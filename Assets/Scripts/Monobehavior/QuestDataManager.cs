using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OngoingQuestData
{
    public QuestData questData;
    public int currentProgress;
    public int maxProgress;
    public bool autoClaimRewards;

    public bool isCompleted;
    public bool rewardClaimed;
}

public class QuestDataManager : MonoBehaviour
{
    public static QuestDataManager Instance { get; private set; }

    public event Action OnQuestsChanged;

    public List<OngoingQuestData> ongoingQuests = new List<OngoingQuestData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddProgress(int index, int value)
    {
        if (!IsValidQuestIndex(index) || value <= 0)
        {
            return;
        }

        OngoingQuestData quest = ongoingQuests[index];

        if (quest == null || quest.isCompleted)
        {
            return;
        }

        quest.currentProgress += value;

        if (quest.maxProgress > 0)
        {
            quest.currentProgress = Mathf.Min(quest.currentProgress, quest.maxProgress);

            if (quest.currentProgress >= quest.maxProgress)
            {
                quest.isCompleted = true;

                if (quest.autoClaimRewards)
                {
                    ClaimQuestRewards(index);
                }
            }
        }

        OnQuestsChanged?.Invoke();
    }

    public void RemoveProgress(int index, int value)
    {
        if (!IsValidQuestIndex(index) || value <= 0)
        {
            return;
        }

        OngoingQuestData quest = ongoingQuests[index];

        if (quest == null || quest.isCompleted)
        {
            return;
        }

        quest.currentProgress -= value;
        quest.currentProgress = Mathf.Max(quest.currentProgress, 0);

        OnQuestsChanged?.Invoke();
    }

    public void ClaimQuestRewards(int index)
    {
        if (!IsValidQuestIndex(index))
        {
            return;
        }

        OngoingQuestData currentQuest = ongoingQuests[index];

        if (currentQuest == null || currentQuest.questData == null)
        {
            return;
        }

        if (!currentQuest.isCompleted)
        {
            return;
        }

        if (currentQuest.rewardClaimed)
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("Cannot claim quest rewards because InventoryManager.Instance is null.");
            return;
        }

        List<QuestReward> rewards = currentQuest.questData.questRewards;

        if (rewards == null)
        {
            currentQuest.rewardClaimed = true;
            OnQuestsChanged?.Invoke();
            return;
        }

        foreach (QuestReward reward in rewards)
        {
            if (reward == null || reward.item == null || reward.quantity <= 0)
            {
                continue;
            }

            InventoryManager.Instance.AddItem(reward.item, reward.quantity);
        }

        currentQuest.rewardClaimed = true;

        OnQuestsChanged?.Invoke();
    }

    private bool IsValidQuestIndex(int index)
    {
        return ongoingQuests != null && index >= 0 && index < ongoingQuests.Count;
    }
}