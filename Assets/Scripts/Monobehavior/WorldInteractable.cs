using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class WorldInteractable : MonoBehaviour
{
    [Header("Interactable Data")]
    [SerializeField] private InteractableData interactableData;
    [SerializeField] private string areaId;
    [SerializeField] private string interactableSaveId;

    [Header("Interaction State")]
    public bool canInteract;

    [Header("Highlight Settings")]
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineScale = 1.08f;

    private SpriteRenderer mainSpriteRenderer;
    private SpriteRenderer outlineSpriteRenderer;
    private GameObject outlineObject;

    private bool isInteracting;

    private void Awake()
    {
        mainSpriteRenderer = GetComponent<SpriteRenderer>();

        if (mainSpriteRenderer == null)
        {
            Debug.LogWarning($"{name} has no SpriteRenderer, so it cannot create an outline.");
            return;
        }

        CreateOutlineObject();
        SetHighlight(false);
    }

    private void LateUpdate()
    {
        // Keeps outline correct if the interactable uses animated sprites.
        if (mainSpriteRenderer != null && outlineSpriteRenderer != null)
        {
            outlineSpriteRenderer.sprite = mainSpriteRenderer.sprite;
            outlineSpriteRenderer.sortingLayerID = mainSpriteRenderer.sortingLayerID;
            outlineSpriteRenderer.sortingOrder = mainSpriteRenderer.sortingOrder - 1;
        }
    }

    public void Initialize(InteractableData data, string areaId, string interactableSaveId)
    {
        this.interactableData = data;
        this.areaId = areaId;
        this.interactableSaveId = interactableSaveId;
    }

    public string GetInteractableSaveId()
    {
        return interactableSaveId;
    }

    public void Interact()
    {
        if (!canInteract)
        {
            return;
        }

        if (isInteracting)
        {
            return;
        }

        if (interactableData == null)
        {
            Debug.LogWarning($"{name} has no InteractableData.");
            return;
        }

        if (!MeetsRequirements())
        {
            Debug.Log($"Cannot interact with {interactableData.interactableId} because requirements are not met.");
            return;
        }

        isInteracting = true;

        try
        {
            ExecuteInteraction();

            if (interactableData.canOnlyInteractOnce)
            {
                SpawnManager.Instance?.MarkInteractableClaimed(areaId, interactableSaveId);
                GameSaveManager.Instance?.RequestSave();

                Destroy(gameObject);
                return;
            }
        }
        finally
        {
            isInteracting = false;
        }
    }

    public void Highlight()
    {
        SetHighlight(true);
    }

    public void Unhighlight()
    {
        SetHighlight(false);
    }

    public void SetHighlight(bool highlighted)
    {
        canInteract = highlighted;

        if (outlineObject != null)
        {
            outlineObject.SetActive(highlighted);
        }
    }

    private void CreateOutlineObject()
    {
        if (mainSpriteRenderer == null)
        {
            return;
        }

        outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineScale;

        outlineSpriteRenderer = outlineObject.AddComponent<SpriteRenderer>();
        outlineSpriteRenderer.sprite = mainSpriteRenderer.sprite;
        outlineSpriteRenderer.color = outlineColor;

        outlineSpriteRenderer.sortingLayerID = mainSpriteRenderer.sortingLayerID;
        outlineSpriteRenderer.sortingOrder = mainSpriteRenderer.sortingOrder - 1;

        outlineObject.SetActive(false);
    }

    private bool MeetsRequirements()
    {
        foreach (string flag in GetStringList(interactableData, "requiredFlags"))
        {
            if (!StoryFlagManager.HasFlag(GameIdUtility.Normalize(flag)))
            {
                return false;
            }
        }

        return true;
    }

    private void ExecuteInteraction()
    {
        if (interactableData == null)
        {
            return;
        }

        ExecuteFlagInteraction();
        ExecuteItemInteraction();
        ExecuteQuestInteraction();

        // Direct cinematic/combat calls are now enabled.
        // If an interactable starts a cinematic that itself ends in combat, leave combatInteraction empty here.
        ExecuteSceneInteraction();
        ExecuteCombatInteraction();

        GameSaveManager.Instance?.RequestSave();

        Debug.Log($"Interacted with {interactableData.interactableId}");
    }

    private void ExecuteSceneInteraction()
    {
        SceneInteraction sceneInteraction = interactableData.sceneInteraction;

        if (sceneInteraction == null || sceneInteraction.scene == null)
        {
            return;
        }

        if (FilmSceneManager.Instance == null)
        {
            Debug.LogWarning($"Cannot start interactable scene '{sceneInteraction.scene.name}' because FilmSceneManager is missing.");
            return;
        }

        bool started = FilmSceneManager.Instance.TryStartCinematic(sceneInteraction.scene);

        if (started)
        {
            Debug.Log($"Interactable started cinematic: {sceneInteraction.scene.sceneId}");
        }
    }

    private void ExecuteCombatInteraction()
    {
        CombatInteraction combatInteraction = interactableData.combatInteraction;

        if (combatInteraction == null ||
            combatInteraction.enemies == null ||
            combatInteraction.enemies.Count == 0)
        {
            return;
        }

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager == null)
        {
            Debug.LogWarning("Cannot start interactable combat because SpawnManager is missing.");
            return;
        }

        string combatId = !string.IsNullOrWhiteSpace(combatInteraction.combatId)
            ? combatInteraction.combatId.Trim()
            : BuildInteractableCombatId();

        bool started = spawnManager.StartCombatFromEnemyPool(
            combatInteraction.enemies,
            combatId
        );

        if (started)
        {
            Debug.Log($"Interactable started combat: {combatId}");
        }
    }

    private string BuildInteractableCombatId()
    {
        string id = !string.IsNullOrWhiteSpace(interactableSaveId)
            ? interactableSaveId
            : interactableData != null
                ? GameIdUtility.GetInteractableId(interactableData)
                : name;

        string sourceAreaId = !string.IsNullOrWhiteSpace(areaId)
            ? areaId
            : SpawnManager.Instance != null
                ? SpawnManager.Instance.currentAreaId
                : "unknown_area";

        return GameIdUtility.Build("interactable", sourceAreaId, id);
    }

    private void ExecuteFlagInteraction()
    {
        // Supports either "flags" or "flagsToSet".
        SetFlagsFromListName("flags");
        SetFlagsFromListName("flagsToSet");
    }

    private void SetFlagsFromListName(string memberName)
    {
        foreach (string flag in GetStringList(interactableData, memberName))
        {
            StoryFlagManager.SetFlag(GameIdUtility.Normalize(flag));
            Debug.Log($"Story flag set: {flag}");
        }
    }

    private void ExecuteItemInteraction()
    {
        object itemInteraction = GetMemberValue(interactableData, "itemInteraction");

        if (itemInteraction == null)
        {
            return;
        }

        object lootObject =
            GetMemberValue(itemInteraction, "loot") ??
            GetMemberValue(itemInteraction, "items") ??
            GetMemberValue(itemInteraction, "itemRewards");

        if (lootObject == null)
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("Cannot give interactable item rewards because InventoryManager.Instance is null.");
            return;
        }

        foreach (object lootEntry in AsEnumerable(lootObject))
        {
            ItemData item = null;
            int quantity = 1;

            // Supports List<ItemData>.
            if (lootEntry is ItemData directItem)
            {
                item = directItem;
                quantity = 1;
            }
            else
            {
                // Supports reward classes like:
                // public ItemData item;
                // public int quantity;
                item =
                    GetMemberValue(lootEntry, "item") as ItemData ??
                    GetMemberValue(lootEntry, "itemData") as ItemData;

                quantity = GetIntMember(lootEntry, "quantity", 1);
            }

            if (item == null)
            {
                continue;
            }

            quantity = Mathf.Max(1, quantity);

            bool added = InventoryManager.Instance.AddItem(item, quantity);

            if (added)
            {
                QuestDataManager.Instance?.ReportItemCollected(item, quantity);
                Debug.Log($"Interactable gave item: {item.itemName} x{quantity}");
            }
        }
    }

    private void ExecuteQuestInteraction()
    {
        // Supports both the correct spelling and your earlier typo: questInteracction.
        object questInteraction =
            GetMemberValue(interactableData, "questInteraction") ??
            GetMemberValue(interactableData, "questInteracction");

        if (questInteraction == null)
        {
            return;
        }

        QuestData questData =
            GetMemberValue(questInteraction, "questData") as QuestData ??
            GetMemberValue(questInteraction, "quest") as QuestData;

        if (questData == null)
        {
            return;
        }

        if (QuestDataManager.Instance == null)
        {
            Debug.LogWarning("Cannot execute quest interaction because QuestDataManager.Instance is null.");
            return;
        }

        bool addQuest = GetBoolMember(questInteraction, "addQuest", true);
        bool completeQuest = GetBoolMember(questInteraction, "completeQuest", false);
        bool claimRewards = GetBoolMember(questInteraction, "claimRewards", false);
        bool autoClaimRewards = GetBoolMember(questInteraction, "autoClaimRewards", false);
        int progressAmount = GetIntMember(questInteraction, "progressAmount", 0);

        int questIndex = FindOngoingQuestIndex(questData);

        if (addQuest && questIndex < 0)
        {
            QuestDataManager.Instance.AddQuest(questData, autoClaimRewards);
            questIndex = FindOngoingQuestIndex(questData);
        }

        if (questIndex >= 0 && progressAmount > 0)
        {
            QuestDataManager.Instance.AddProgress(questIndex, progressAmount);
        }

        if (questIndex >= 0 && completeQuest)
        {
            QuestDataManager.Instance.CompleteQuest(questIndex);
        }

        if (questIndex >= 0 && claimRewards)
        {
            QuestDataManager.Instance.ClaimQuestRewards(questIndex);
        }
    }

    private int FindOngoingQuestIndex(QuestData questData)
    {
        if (QuestDataManager.Instance == null || questData == null)
        {
            return -1;
        }

        List<OngoingQuestData> ongoingQuests = QuestDataManager.Instance.ongoingQuests;

        if (ongoingQuests == null)
        {
            return -1;
        }

        string targetQuestId = GetQuestId(questData);

        for (int i = 0; i < ongoingQuests.Count; i++)
        {
            OngoingQuestData ongoingQuest = ongoingQuests[i];

            if (ongoingQuest == null || ongoingQuest.questData == null)
            {
                continue;
            }

            if (ongoingQuest.questData == questData)
            {
                return i;
            }

            if (!string.IsNullOrWhiteSpace(targetQuestId) &&
                GetQuestId(ongoingQuest.questData) == targetQuestId)
            {
                return i;
            }
        }

        return -1;
    }

    private string GetQuestId(QuestData questData)
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

    private object GetMemberValue(object target, string memberName)
    {
        if (target == null || string.IsNullOrWhiteSpace(memberName))
        {
            return null;
        }

        Type type = target.GetType();

        FieldInfo field = type.GetField(
            memberName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (field != null)
        {
            return field.GetValue(target);
        }

        PropertyInfo property = type.GetProperty(
            memberName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (property != null && property.CanRead)
        {
            return property.GetValue(target);
        }

        return null;
    }

    private IEnumerable AsEnumerable(object value)
    {
        if (value == null)
        {
            yield break;
        }

        if (value is string)
        {
            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (object entry in enumerable)
            {
                yield return entry;
            }
        }
    }

    private List<string> GetStringList(object target, string memberName)
    {
        List<string> result = new List<string>();

        object value = GetMemberValue(target, memberName);

        if (value == null)
        {
            return result;
        }

        if (value is string singleString)
        {
            if (!string.IsNullOrWhiteSpace(singleString))
            {
                result.Add(singleString);
            }

            return result;
        }

        foreach (object entry in AsEnumerable(value))
        {
            if (entry is string stringEntry && !string.IsNullOrWhiteSpace(stringEntry))
            {
                result.Add(stringEntry);
            }
        }

        return result;
    }

    private bool GetBoolMember(object target, string memberName, bool defaultValue)
    {
        object value = GetMemberValue(target, memberName);

        if (value == null)
        {
            return defaultValue;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(value.ToString(), out bool parsedValue))
        {
            return parsedValue;
        }

        return defaultValue;
    }

    private int GetIntMember(object target, string memberName, int defaultValue)
    {
        object value = GetMemberValue(target, memberName);

        if (value == null)
        {
            return defaultValue;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (int.TryParse(value.ToString(), out int parsedValue))
        {
            return parsedValue;
        }

        return defaultValue;
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

public static class StoryFlagManager
{
    private const string StoryFlagsKey = "StoryFlags";

    public static void SetFlag(string flag)
    {
        flag = NormalizeFlag(flag);

        if (string.IsNullOrWhiteSpace(flag))
        {
            return;
        }

        HashSet<string> flags = LoadFlags();

        if (flags.Add(flag))
        {
            SaveFlags(flags);
        }
    }

    public static bool HasFlag(string flag)
    {
        flag = NormalizeFlag(flag);

        if (string.IsNullOrWhiteSpace(flag))
        {
            return false;
        }

        HashSet<string> flags = LoadFlags();
        return flags.Contains(flag);
    }

    public static void RemoveFlag(string flag)
    {
        flag = NormalizeFlag(flag);

        if (string.IsNullOrWhiteSpace(flag))
        {
            return;
        }

        HashSet<string> flags = LoadFlags();

        if (flags.Remove(flag))
        {
            SaveFlags(flags);
        }
    }

    public static void ClearFlags()
    {
        PlayerPrefs.DeleteKey(StoryFlagsKey);
        PlayerPrefs.Save();

        Debug.Log("Story flags cleared.");
    }

    private static HashSet<string> LoadFlags()
    {
        HashSet<string> flags = new HashSet<string>();

        string saveString = PlayerPrefs.GetString(StoryFlagsKey, "");

        if (string.IsNullOrWhiteSpace(saveString))
        {
            return flags;
        }

        string[] entries = saveString.Split(';');

        foreach (string entry in entries)
        {
            string normalizedEntry = NormalizeFlag(entry);

            if (!string.IsNullOrWhiteSpace(normalizedEntry))
            {
                flags.Add(normalizedEntry);
            }
        }

        return flags;
    }

    private static void SaveFlags(HashSet<string> flags)
    {
        string saveString = string.Join(";", flags);
        PlayerPrefs.SetString(StoryFlagsKey, saveString);
        PlayerPrefs.Save();
    }

    private static string NormalizeFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return "";
        }

        return flag.Trim().ToLowerInvariant();
    }
}
