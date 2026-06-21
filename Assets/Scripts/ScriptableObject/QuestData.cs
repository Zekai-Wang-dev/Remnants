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
    public string questName;
    public QuestType questType;
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