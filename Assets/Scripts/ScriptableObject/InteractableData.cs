using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableData", menuName = "Game/Interactable Data")]
public class InteractableData : ScriptableObject
{
    [Header("ID")]
    [Tooltip("Stable ID used by saves and generated interactable combat IDs. Auto-filled from asset name if blank.")]
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

    public string Id => GameIdUtility.GetInteractableId(this);

    private void OnValidate()
    {
        interactableId = GameIdUtility.CleanOrCreate(interactableId, this);
    }
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
