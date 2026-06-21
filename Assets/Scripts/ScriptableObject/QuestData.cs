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
    public string questId;
    public string questName;
    public QuestType questType;

    [Header("Requirement")]
    public QuestRequirementType questRequirementType;

    public EnemyDefeatRequirement enemyDefeatRequirement = new EnemyDefeatRequirement();
    public ItemCollectionRequirement itemCollectionRequirement = new ItemCollectionRequirement();
    public LocationVisitRequirement locationVisitRequirement = new LocationVisitRequirement();
    public TalkToNPCRequirement talkToNPCRequirement = new TalkToNPCRequirement();

    [Header("Rewards")]
    public List<QuestReward> questRewards = new List<QuestReward>();
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
    [Tooltip("For fixed encounters, use EncounterTrigger.encounterId / EnemyEntry.spawnPointId. For random encounters, use the enemy id/name without the random_ prefix.")]
    public string enemyId;

    public int requiredAmount = 1;
}

[Serializable]
public class ItemCollectionRequirement
{
    public string itemId;
    public int requiredAmount = 1;
}

[Serializable]
public class LocationVisitRequirement
{
    public string sceneId;
    public string areaId;
    public int requiredAmount = 1;
}

[Serializable]
public class TalkToNPCRequirement
{
    public string npcId;
    public string sceneId;
    public int requiredAmount = 1;
}