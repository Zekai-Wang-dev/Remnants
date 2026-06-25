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

[Serializable]
public class QuestSaveData
{
    public List<SavedQuestState> questStates = new List<SavedQuestState>();
}

[Serializable]
public class SavedQuestState
{
    public string questId;
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

    private readonly HashSet<string> completedQuestIds = new HashSet<string>();
    private readonly HashSet<string> rewardClaimedQuestIds = new HashSet<string>();
    private readonly Dictionary<string, SavedQuestState> savedQuestStateById = new Dictionary<string, SavedQuestState>();

    private bool isLoadingFromSave;

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

    public void AddQuest(QuestData questData, bool autoClaimRewards = false)
    {
        if (questData == null)
        {
            return;
        }

        OngoingQuestData newQuest = new OngoingQuestData
        {
            questData = questData,
            currentProgress = 0,
            maxProgress = GetRequiredProgress(questData),
            autoClaimRewards = autoClaimRewards,
            isCompleted = false,
            rewardClaimed = false
        };

        AddQuest(newQuest);
    }

    public void AddQuest(OngoingQuestData newQuest)
    {
        if (newQuest == null || newQuest.questData == null)
        {
            return;
        }

        string questId = GetQuestId(newQuest.questData);

        if (string.IsNullOrWhiteSpace(questId))
        {
            Debug.LogWarning($"Cannot add quest '{newQuest.questData.name}' because it has no questId.");
            return;
        }

        if (completedQuestIds.Contains(questId) && rewardClaimedQuestIds.Contains(questId))
        {
            return;
        }

        if (HasQuest(newQuest.questData))
        {
            return;
        }

        OngoingQuestData runtimeQuest;

        if (savedQuestStateById.TryGetValue(questId, out SavedQuestState savedState))
        {
            if (savedState.isCompleted && savedState.rewardClaimed)
            {
                return;
            }

            runtimeQuest = CreateRuntimeQuestFromSavedState(newQuest.questData, savedState);
        }
        else
        {
            runtimeQuest = CreateRuntimeQuestCopy(newQuest);
        }

        ongoingQuests.Add(runtimeQuest);
        SaveRuntimeQuestState(runtimeQuest);

        NotifyQuestsChanged(true);
    }

    public void ClearRuntimeQuestData()
    {
        isLoadingFromSave = true;

        ongoingQuests.Clear();
        completedQuestIds.Clear();
        rewardClaimedQuestIds.Clear();
        savedQuestStateById.Clear();

        isLoadingFromSave = false;

        NotifyQuestsChanged(false);

        Debug.Log("Quest runtime data cleared.");
    }

    public void ReportEnemyDefeated(string enemyId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(enemyId) || amount <= 0)
        {
            return;
        }

        int questCount = ongoingQuests.Count;

        for (int i = 0; i < questCount; i++)
        {
            if (!IsValidQuestIndex(i))
            {
                continue;
            }

            OngoingQuestData quest = ongoingQuests[i];

            if (!CanProgressQuest(quest))
            {
                continue;
            }

            QuestData questData = quest.questData;

            if (questData.questRequirementType != QuestRequirementType.DefeatEnemy)
            {
                continue;
            }

            EnemyDefeatRequirement requirement = questData.enemyDefeatRequirement;

            if (requirement == null)
            {
                continue;
            }

            if (IdMatches(requirement.enemyId, enemyId, true))
            {
                AddProgress(i, amount);
            }
        }
    }

    public void ReportItemCollected(ItemData item, int quantity = 1)
    {
        if (item == null)
        {
            return;
        }

        ReportItemCollected(item.itemId, quantity);
    }

    public void ReportItemCollected(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return;
        }

        int questCount = ongoingQuests.Count;

        for (int i = 0; i < questCount; i++)
        {
            if (!IsValidQuestIndex(i))
            {
                continue;
            }

            OngoingQuestData quest = ongoingQuests[i];

            if (!CanProgressQuest(quest))
            {
                continue;
            }

            QuestData questData = quest.questData;

            if (questData.questRequirementType != QuestRequirementType.CollectItem)
            {
                continue;
            }

            ItemCollectionRequirement requirement = questData.itemCollectionRequirement;

            if (requirement == null)
            {
                continue;
            }

            if (IdMatches(requirement.itemId, itemId, false))
            {
                AddProgress(i, quantity);
            }
        }
    }

    public void ReportLocationVisited(string sceneId, string areaId, int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        int questCount = ongoingQuests.Count;

        for (int i = 0; i < questCount; i++)
        {
            if (!IsValidQuestIndex(i))
            {
                continue;
            }

            OngoingQuestData quest = ongoingQuests[i];

            if (!CanProgressQuest(quest))
            {
                continue;
            }

            QuestData questData = quest.questData;

            if (questData.questRequirementType != QuestRequirementType.VisitLocation)
            {
                continue;
            }

            LocationVisitRequirement requirement = questData.locationVisitRequirement;

            if (requirement == null)
            {
                continue;
            }

            bool hasSceneRequirement = !string.IsNullOrWhiteSpace(requirement.sceneId);
            bool hasAreaRequirement = !string.IsNullOrWhiteSpace(requirement.areaId);

            if (!hasSceneRequirement && !hasAreaRequirement)
            {
                continue;
            }

            bool sceneMatches = !hasSceneRequirement || IdMatches(requirement.sceneId, sceneId, false);
            bool areaMatches = !hasAreaRequirement || IdMatches(requirement.areaId, areaId, false);

            if (sceneMatches && areaMatches)
            {
                AddProgress(i, amount);
            }
        }
    }

    public void ReportTalkToNPC(string npcId, string sceneId = "", int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        int questCount = ongoingQuests.Count;

        for (int i = 0; i < questCount; i++)
        {
            if (!IsValidQuestIndex(i))
            {
                continue;
            }

            OngoingQuestData quest = ongoingQuests[i];

            if (!CanProgressQuest(quest))
            {
                continue;
            }

            QuestData questData = quest.questData;

            if (questData.questRequirementType != QuestRequirementType.TalkToNPC)
            {
                continue;
            }

            TalkToNPCRequirement requirement = questData.talkToNPCRequirement;

            if (requirement == null)
            {
                continue;
            }

            bool hasNpcRequirement = !string.IsNullOrWhiteSpace(requirement.npcId);
            bool hasSceneRequirement = !string.IsNullOrWhiteSpace(requirement.sceneId);

            if (!hasNpcRequirement && !hasSceneRequirement)
            {
                continue;
            }

            bool npcMatches = !hasNpcRequirement || IdMatches(requirement.npcId, npcId, false);
            bool sceneMatches = !hasSceneRequirement || IdMatches(requirement.sceneId, sceneId, false);

            if (npcMatches && sceneMatches)
            {
                AddProgress(i, amount);
            }
        }
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

        if (quest.maxProgress <= 0)
        {
            quest.maxProgress = GetRequiredProgress(quest.questData);
        }

        quest.currentProgress += value;
        quest.currentProgress = Mathf.Min(quest.currentProgress, quest.maxProgress);

        if (quest.currentProgress >= quest.maxProgress)
        {
            CompleteQuest(index);
            return;
        }

        SaveRuntimeQuestState(quest);
        NotifyQuestsChanged(true);
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

        SaveRuntimeQuestState(quest);
        NotifyQuestsChanged(true);
    }

    public void CompleteQuest(int index)
    {
        if (!IsValidQuestIndex(index))
        {
            return;
        }

        OngoingQuestData quest = ongoingQuests[index];

        if (quest == null || quest.isCompleted)
        {
            return;
        }

        if (quest.maxProgress <= 0)
        {
            quest.maxProgress = GetRequiredProgress(quest.questData);
        }

        quest.isCompleted = true;
        quest.currentProgress = Mathf.Max(quest.currentProgress, quest.maxProgress);

        string questId = GetQuestId(quest.questData);

        if (!string.IsNullOrWhiteSpace(questId))
        {
            completedQuestIds.Add(questId);
        }

        SaveRuntimeQuestState(quest);
        TryQueueCompletionCinematic(quest);

        if (quest.autoClaimRewards)
        {
            ClaimQuestRewards(index);
            return;
        }

        NotifyQuestsChanged(true);
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

            SaveRuntimeQuestState(currentQuest);
            NotifyQuestsChanged(true);
            return;
        }

        List<QuestReward> rewards = currentQuest.questData.questRewards;

        if (rewards != null)
        {
            foreach (QuestReward reward in rewards)
            {
                if (reward == null || reward.item == null || reward.quantity <= 0)
                {
                    continue;
                }

                InventoryManager.Instance.AddItem(reward.item, reward.quantity);
            }
        }

        currentQuest.rewardClaimed = true;

        string questId = GetQuestId(currentQuest.questData);

        if (!string.IsNullOrWhiteSpace(questId))
        {
            completedQuestIds.Add(questId);
            rewardClaimedQuestIds.Add(questId);
        }

        SaveRuntimeQuestState(currentQuest);
        TryAddChainedQuest(currentQuest);

        NotifyQuestsChanged(true);
    }

    private void TryQueueCompletionCinematic(OngoingQuestData completedQuest)
    {
        if (completedQuest == null || completedQuest.questData == null)
        {
            return;
        }

        QuestData completedQuestData = completedQuest.questData;

        if (!completedQuestData.triggerCinematicOnComplete)
        {
            return;
        }

        if (completedQuestData.cinematicOnComplete == null)
        {
            Debug.LogWarning($"Quest '{completedQuestData.name}' is marked to trigger a completion cinematic, but no cinematic is assigned.");
            return;
        }

        if (FilmSceneManager.Instance == null)
        {
            Debug.LogWarning($"Quest '{completedQuestData.name}' tried to queue a completion cinematic, but FilmSceneManager is missing.");
            return;
        }

        FilmSceneManager.Instance.QueueCinematic(completedQuestData.cinematicOnComplete);
    }

    private void TryAddChainedQuest(OngoingQuestData completedQuest)
    {
        if (completedQuest == null || completedQuest.questData == null)
        {
            return;
        }

        QuestData completedQuestData = completedQuest.questData;

        if (!completedQuestData.isChained)
        {
            return;
        }

        if (completedQuestData.newQuest == null)
        {
            Debug.LogWarning($"Quest '{completedQuestData.name}' is marked as chained, but no newQuest is assigned.");
            return;
        }

        string completedQuestId = GetQuestId(completedQuestData);
        string newQuestId = GetQuestId(completedQuestData.newQuest);

        if (!string.IsNullOrWhiteSpace(completedQuestId) &&
            !string.IsNullOrWhiteSpace(newQuestId) &&
            completedQuestId == newQuestId)
        {
            Debug.LogWarning($"Quest '{completedQuestData.name}' cannot chain into itself.");
            return;
        }

        AddQuest(completedQuestData.newQuest, completedQuestData.newQuestAutoClaimRewards);
    }

    public QuestSaveData CreateSaveData()
    {
        QuestSaveData saveData = new QuestSaveData();

        Dictionary<string, SavedQuestState> combinedStates =
            new Dictionary<string, SavedQuestState>();

        foreach (KeyValuePair<string, SavedQuestState> pair in savedQuestStateById)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
            {
                continue;
            }

            combinedStates[pair.Key] = CopySavedState(pair.Value);
        }

        foreach (OngoingQuestData quest in ongoingQuests)
        {
            if (quest == null || quest.questData == null)
            {
                continue;
            }

            string questId = GetQuestId(quest.questData);

            if (string.IsNullOrWhiteSpace(questId))
            {
                continue;
            }

            combinedStates[questId] = CreateSavedStateFromRuntimeQuest(quest);
        }

        foreach (string questId in completedQuestIds)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                continue;
            }

            if (!combinedStates.ContainsKey(questId))
            {
                combinedStates[questId] = new SavedQuestState
                {
                    questId = questId,
                    currentProgress = 1,
                    maxProgress = 1,
                    autoClaimRewards = false,
                    isCompleted = true,
                    rewardClaimed = rewardClaimedQuestIds.Contains(questId)
                };
            }
        }

        foreach (SavedQuestState state in combinedStates.Values)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.questId))
            {
                continue;
            }

            saveData.questStates.Add(state);
        }

        return saveData;
    }

    public void LoadFromSaveData(QuestSaveData saveData, QuestDatabase questDatabase)
    {
        isLoadingFromSave = true;

        ongoingQuests.Clear();
        completedQuestIds.Clear();
        rewardClaimedQuestIds.Clear();
        savedQuestStateById.Clear();

        if (saveData == null || saveData.questStates == null)
        {
            isLoadingFromSave = false;
            NotifyQuestsChanged(false);
            return;
        }

        if (questDatabase == null)
        {
            Debug.LogWarning("Cannot load quests because QuestDatabase is null.");
            isLoadingFromSave = false;
            NotifyQuestsChanged(false);
            return;
        }

        questDatabase.Initialize();

        foreach (SavedQuestState savedState in saveData.questStates)
        {
            if (savedState == null || string.IsNullOrWhiteSpace(savedState.questId))
            {
                continue;
            }

            string questId = NormalizeId(savedState.questId, false);

            SavedQuestState normalizedSavedState = CopySavedState(savedState);
            normalizedSavedState.questId = questId;

            savedQuestStateById[questId] = normalizedSavedState;

            if (normalizedSavedState.isCompleted)
            {
                completedQuestIds.Add(questId);
            }

            if (normalizedSavedState.rewardClaimed)
            {
                rewardClaimedQuestIds.Add(questId);
            }

            if (normalizedSavedState.isCompleted && normalizedSavedState.rewardClaimed)
            {
                continue;
            }

            QuestData questData = questDatabase.GetQuestById(questId);

            if (questData == null)
            {
                Debug.LogWarning($"Could not restore quest because QuestDatabase has no quest with id: {questId}");
                continue;
            }

            OngoingQuestData restoredQuest = CreateRuntimeQuestFromSavedState(
                questData,
                normalizedSavedState
            );

            ongoingQuests.Add(restoredQuest);
        }

        isLoadingFromSave = false;
        NotifyQuestsChanged(false);
    }

    private OngoingQuestData CreateRuntimeQuestCopy(OngoingQuestData source)
    {
        OngoingQuestData runtimeQuest = new OngoingQuestData
        {
            questData = source.questData,
            currentProgress = Mathf.Max(0, source.currentProgress),
            maxProgress = source.maxProgress,
            autoClaimRewards = source.autoClaimRewards,
            isCompleted = source.isCompleted,
            rewardClaimed = source.rewardClaimed
        };

        if (runtimeQuest.maxProgress <= 0)
        {
            runtimeQuest.maxProgress = GetRequiredProgress(runtimeQuest.questData);
        }

        runtimeQuest.currentProgress = Mathf.Min(
            runtimeQuest.currentProgress,
            runtimeQuest.maxProgress
        );

        return runtimeQuest;
    }

    private OngoingQuestData CreateRuntimeQuestFromSavedState(
        QuestData questData,
        SavedQuestState savedState)
    {
        OngoingQuestData runtimeQuest = new OngoingQuestData
        {
            questData = questData,
            currentProgress = Mathf.Max(0, savedState.currentProgress),
            maxProgress = savedState.maxProgress,
            autoClaimRewards = savedState.autoClaimRewards,
            isCompleted = savedState.isCompleted,
            rewardClaimed = savedState.rewardClaimed
        };

        if (runtimeQuest.maxProgress <= 0)
        {
            runtimeQuest.maxProgress = GetRequiredProgress(runtimeQuest.questData);
        }

        runtimeQuest.currentProgress = Mathf.Min(
            runtimeQuest.currentProgress,
            runtimeQuest.maxProgress
        );

        return runtimeQuest;
    }

    private SavedQuestState CreateSavedStateFromRuntimeQuest(OngoingQuestData quest)
    {
        string questId = GetQuestId(quest.questData);

        return new SavedQuestState
        {
            questId = questId,
            currentProgress = quest.currentProgress,
            maxProgress = quest.maxProgress,
            autoClaimRewards = quest.autoClaimRewards,
            isCompleted = quest.isCompleted,
            rewardClaimed = quest.rewardClaimed
        };
    }

    private SavedQuestState CopySavedState(SavedQuestState source)
    {
        return new SavedQuestState
        {
            questId = NormalizeId(source.questId, false),
            currentProgress = source.currentProgress,
            maxProgress = source.maxProgress,
            autoClaimRewards = source.autoClaimRewards,
            isCompleted = source.isCompleted,
            rewardClaimed = source.rewardClaimed
        };
    }

    private void SaveRuntimeQuestState(OngoingQuestData quest)
    {
        if (quest == null || quest.questData == null)
        {
            return;
        }

        string questId = GetQuestId(quest.questData);

        if (string.IsNullOrWhiteSpace(questId))
        {
            return;
        }

        savedQuestStateById[questId] = CreateSavedStateFromRuntimeQuest(quest);

        if (quest.isCompleted)
        {
            completedQuestIds.Add(questId);
        }

        if (quest.rewardClaimed)
        {
            rewardClaimedQuestIds.Add(questId);
        }
    }

    private int GetRequiredProgress(QuestData questData)
    {
        if (questData == null)
        {
            return 1;
        }

        switch (questData.questRequirementType)
        {
            case QuestRequirementType.DefeatEnemy:
                return GetSafeRequirementAmount(questData.enemyDefeatRequirement != null
                    ? questData.enemyDefeatRequirement.requiredAmount
                    : 1);

            case QuestRequirementType.CollectItem:
                return GetSafeRequirementAmount(questData.itemCollectionRequirement != null
                    ? questData.itemCollectionRequirement.requiredAmount
                    : 1);

            case QuestRequirementType.VisitLocation:
                return GetSafeRequirementAmount(questData.locationVisitRequirement != null
                    ? questData.locationVisitRequirement.requiredAmount
                    : 1);

            case QuestRequirementType.TalkToNPC:
                return GetSafeRequirementAmount(questData.talkToNPCRequirement != null
                    ? questData.talkToNPCRequirement.requiredAmount
                    : 1);

            default:
                return 1;
        }
    }

    private int GetSafeRequirementAmount(int amount)
    {
        return Mathf.Max(1, amount);
    }

    private bool HasQuest(QuestData questData)
    {
        if (questData == null)
        {
            return false;
        }

        string newQuestId = GetQuestId(questData);

        foreach (OngoingQuestData quest in ongoingQuests)
        {
            if (quest == null || quest.questData == null)
            {
                continue;
            }

            if (GetQuestId(quest.questData) == newQuestId)
            {
                return true;
            }
        }

        return false;
    }

    private string GetQuestId(QuestData questData)
    {
        if (questData == null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(questData.questId))
        {
            return NormalizeId(questData.questId, false);
        }

        return NormalizeId(questData.name, false);
    }

    private bool CanProgressQuest(OngoingQuestData quest)
    {
        return quest != null &&
               quest.questData != null &&
               !quest.isCompleted;
    }

    private bool IsValidQuestIndex(int index)
    {
        return ongoingQuests != null && index >= 0 && index < ongoingQuests.Count;
    }

    private bool IdMatches(string requiredId, string actualId, bool stripRandomPrefix)
    {
        if (string.IsNullOrWhiteSpace(requiredId) || string.IsNullOrWhiteSpace(actualId))
        {
            return false;
        }

        return NormalizeId(requiredId, stripRandomPrefix) ==
               NormalizeId(actualId, stripRandomPrefix);
    }

    private string NormalizeId(string value, bool stripRandomPrefix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (stripRandomPrefix && normalized.StartsWith("random_"))
        {
            normalized = normalized.Substring("random_".Length);
        }

        return normalized;
    }

    private void NotifyQuestsChanged(bool save)
    {
        OnQuestsChanged?.Invoke();

        if (!save)
        {
            return;
        }

        if (isLoadingFromSave)
        {
            return;
        }

        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.RequestSave();
        }
    }
}