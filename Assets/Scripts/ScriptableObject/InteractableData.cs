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
    public bool canRefresh = false;

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
    [Tooltip("Optional. If empty, one will be generated from the interactable or cinematic scene.")]
    public string combatId;

    [Tooltip("Enemy pool used for this custom combat interaction. These enemies are used directly instead of AreaData fixed/random encounters.")]
    public List<CharacterData> enemies = new();
}
