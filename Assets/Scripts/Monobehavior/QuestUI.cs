using System.Collections.Generic;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private Transform questRoot;
    [SerializeField] private QuestRowUI questPrefab;

    private readonly List<QuestRowUI> activeQuests = new List<QuestRowUI>();

    private QuestDataManager questDataManager;
    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed)
        {
            return;
        }

        questDataManager = QuestDataManager.Instance;

        if (questDataManager == null)
        {
            return;
        }

        questDataManager.OnQuestsChanged += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || questDataManager == null)
        {
            return;
        }

        questDataManager.OnQuestsChanged -= Refresh;
        subscribed = false;
    }

    public void Refresh()
    {
        ClearQuests();

        QuestDataManager manager = questDataManager != null
            ? questDataManager
            : QuestDataManager.Instance;

        if (manager == null || manager.ongoingQuests == null)
        {
            return;
        }

        for (int i = 0; i < manager.ongoingQuests.Count; i++)
        {
            OngoingQuestData quest = manager.ongoingQuests[i];

            if (quest == null || quest.questData == null || quest.rewardClaimed)
            {
                continue;
            }

            AddQuest(
                quest.questData.questName,
                quest.currentProgress,
                quest.maxProgress
            );
        }
    }

    public void AddQuest(string name, int value, int maxValue)
    {
        if (questPrefab == null || questRoot == null)
        {
            return;
        }

        QuestRowUI row = Instantiate(questPrefab, questRoot);
        row.SetQuest(name, value, maxValue);

        activeQuests.Add(row);
    }

    public void RemoveQuest(int index)
    {
        if (index < 0 || index >= activeQuests.Count)
        {
            return;
        }

        QuestRowUI row = activeQuests[index];
        activeQuests.RemoveAt(index);

        if (row != null)
        {
            Destroy(row.gameObject);
        }
    }

    public void ClearQuests()
    {
        for (int i = 0; i < activeQuests.Count; i++)
        {
            if (activeQuests[i] != null)
            {
                Destroy(activeQuests[i].gameObject);
            }
        }

        activeQuests.Clear();
    }
}