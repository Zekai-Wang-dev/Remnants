using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableData", menuName = "Game/Interactable Data")]
public class InteractableData : ScriptableObject
{
    public string interactableId;
    public string displayName;
    public GameObject interactablePrefab;

    [TextArea]
    public string interactionPrompt;

    public bool canOnlyInteractOnce = true;

    public List<string> requiredFlags = new();
    public List<string> flagsToSet = new();

    public ItemInteraction itemInteraction;
    public SceneInteraction sceneInteraction;
    public QuestInteraction questInteraction;
    public CombatInteraction combatInteraction;
}

[System.Serializable]
public class ItemInteraction
{
    public List<ItemData> loot = new();
}

[System.Serializable]
public class SceneInteraction
{
    public SceneData scene;
}

[System.Serializable]
public class QuestInteraction
{
    public QuestData questData;
    public string objectiveId;
    public bool completeQuest;
}

[System.Serializable]
public class CombatInteraction
{
    public List<CharacterData> enemies = new();
}