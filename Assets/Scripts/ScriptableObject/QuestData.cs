using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestReward
{
    public ItemData item;
    public int quantity = 1;
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("Quest Info")]
    [Tooltip("Stable ID used by quest saves. Auto-filled from asset name if blank.")]
    public string questId;

    public string questName;
    public QuestType questType;

    [Header("Quest Chain")]
    [Tooltip("If true, completing this quest will immediately add newQuest.")]
    public bool isChained;

    [Tooltip("If true, completing this quest will queue a completion cinematic.")]
    public bool triggerCinematicOnComplete;

    [Tooltip("Quest added automatically when this quest completes.")]
    public QuestData newQuest;

    [Tooltip("Cinematic to trigger when this quest completes.")]
    public SceneData cinematicOnComplete;

    [Tooltip("Whether the chained quest should auto-claim its own rewards when it completes.")]
    public bool newQuestAutoClaimRewards;

    [Header("Requirement")]
    public QuestRequirementType questRequirementType;

    public EnemyDefeatRequirement enemyDefeatRequirement = new EnemyDefeatRequirement();
    public ItemCollectionRequirement itemCollectionRequirement = new ItemCollectionRequirement();
    public LocationVisitRequirement locationVisitRequirement = new LocationVisitRequirement();
    public TalkToNPCRequirement talkToNPCRequirement = new TalkToNPCRequirement();

    [Header("Rewards")]
    public List<QuestReward> questRewards = new List<QuestReward>();

    public string Id => GameIdUtility.GetQuestId(this);

    private void OnValidate()
    {
        questId = GameIdUtility.CleanOrCreate(questId, this);
    }
}

public enum QuestType
{
    Main,
    Side,
    Daily,
    Event,
    Tutorial
}

public enum QuestRequirementType
{
    None,
    DefeatEnemy,
    CollectItem,
    VisitLocation,
    TalkToNPC
}

[Serializable]
public class EnemyDefeatRequirement
{
    [Tooltip("Use CharacterData.characterId. For random encounters, do not include the random_ prefix.")]
    public string enemyId;

    public int requiredAmount = 1;
}

[Serializable]
public class ItemCollectionRequirement
{
    [Tooltip("Use ItemData.itemId.")]
    public string itemId;
    public int requiredAmount = 1;
}

[Serializable]
public class LocationVisitRequirement
{
    [Tooltip("Use SpawnManager sceneId.")]
    public string sceneId;

    [Tooltip("Use AreaData.areaId.")]
    public string areaId;

    public int requiredAmount = 1;
}

[Serializable]
public class TalkToNPCRequirement
{
    [Tooltip("Use SceneCharacterRegistry actorId.")]
    public string npcId;

    [Tooltip("Optional. Use SceneData.sceneId if you want this conversation to count only during a specific cinematic.")]
    public string sceneId;

    public int requiredAmount = 1;
}
