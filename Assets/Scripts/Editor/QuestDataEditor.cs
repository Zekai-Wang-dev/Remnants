#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestData))]
public class QuestDataEditor : Editor
{
    private SerializedProperty questId;
    private SerializedProperty questName;
    private SerializedProperty questType;

    private SerializedProperty isChained;
    private SerializedProperty triggerCinematicOnComplete;
    private SerializedProperty newQuest;
    private SerializedProperty cinematicOnComplete;
    private SerializedProperty newQuestAutoClaimRewards;

    private SerializedProperty questRequirementType;

    private SerializedProperty enemyDefeatRequirement;
    private SerializedProperty itemCollectionRequirement;
    private SerializedProperty locationVisitRequirement;
    private SerializedProperty talkToNPCRequirement;

    private SerializedProperty questRewards;

    private void OnEnable()
    {
        questId = serializedObject.FindProperty("questId");
        questName = serializedObject.FindProperty("questName");
        questType = serializedObject.FindProperty("questType");

        isChained = serializedObject.FindProperty("isChained");
        triggerCinematicOnComplete = serializedObject.FindProperty("triggerCinematicOnComplete");

        newQuest = serializedObject.FindProperty("newQuest");
        cinematicOnComplete = serializedObject.FindProperty("cinematicOnComplete");
        newQuestAutoClaimRewards = serializedObject.FindProperty("newQuestAutoClaimRewards");

        questRequirementType = serializedObject.FindProperty("questRequirementType");

        enemyDefeatRequirement = serializedObject.FindProperty("enemyDefeatRequirement");
        itemCollectionRequirement = serializedObject.FindProperty("itemCollectionRequirement");
        locationVisitRequirement = serializedObject.FindProperty("locationVisitRequirement");
        talkToNPCRequirement = serializedObject.FindProperty("talkToNPCRequirement");

        questRewards = serializedObject.FindProperty("questRewards");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawQuestInfoSection();
        DrawCompletionActionsSection();
        DrawRequirementSection();
        DrawRewardsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawQuestInfoSection()
    {
        EditorGUILayout.LabelField("Quest Info", EditorStyles.boldLabel);

        DrawOptionalProperty(questId);
        DrawOptionalProperty(questName);
        DrawOptionalProperty(questType);
    }

    private void DrawCompletionActionsSection()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Completion Actions", EditorStyles.boldLabel);

        DrawOptionalProperty(isChained, false, "Add New Quest On Complete");
        DrawOptionalProperty(triggerCinematicOnComplete, false, "Start Cinematic On Complete");

        bool hasChain = isChained != null && isChained.boolValue;
        bool hasCinematic = triggerCinematicOnComplete != null && triggerCinematicOnComplete.boolValue;

        if (!hasChain && !hasCinematic)
        {
            EditorGUILayout.HelpBox(
                "No completion actions are enabled. Completing this quest will only mark it completed and allow reward claiming.",
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (hasChain)
        {
            EditorGUILayout.LabelField("New Quest", EditorStyles.boldLabel);

            DrawOptionalProperty(newQuest, false, "New Quest Data");
            DrawOptionalProperty(newQuestAutoClaimRewards, false, "New Quest Auto Claim Rewards");

            if (newQuest != null && newQuest.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Add New Quest On Complete is enabled, but no new quest is assigned.",
                    MessageType.Warning
                );
            }
        }

        if (hasCinematic)
        {
            if (hasChain)
            {
                EditorGUILayout.Space(6);
            }

            EditorGUILayout.LabelField("Completion Cinematic", EditorStyles.boldLabel);

            DrawOptionalProperty(cinematicOnComplete, false, "Cinematic Scene Data");

            if (cinematicOnComplete != null && cinematicOnComplete.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Start Cinematic On Complete is enabled, but no cinematic SceneData is assigned.",
                    MessageType.Warning
                );
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRequirementSection()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Requirement", EditorStyles.boldLabel);
        DrawOptionalProperty(questRequirementType);

        if (questRequirementType == null)
        {
            return;
        }

        QuestRequirementType requirementType =
            (QuestRequirementType)questRequirementType.enumValueIndex;

        EditorGUILayout.Space(4);

        switch (requirementType)
        {
            case QuestRequirementType.None:
                EditorGUILayout.HelpBox(
                    "This quest has no progression requirement.",
                    MessageType.Info
                );
                break;

            case QuestRequirementType.DefeatEnemy:
                DrawRequirementBox("Defeat Enemy Requirement", enemyDefeatRequirement);
                break;

            case QuestRequirementType.CollectItem:
                DrawRequirementBox("Collect Item Requirement", itemCollectionRequirement);
                break;

            case QuestRequirementType.VisitLocation:
                DrawRequirementBox("Visit Location Requirement", locationVisitRequirement);
                break;

            case QuestRequirementType.TalkToNPC:
                DrawRequirementBox("Talk To NPC Requirement", talkToNPCRequirement);
                break;
        }
    }

    private void DrawRewardsSection()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
        DrawOptionalProperty(questRewards, true);
    }

    private void DrawRequirementBox(string title, SerializedProperty property)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        DrawOptionalProperty(property, true);

        EditorGUILayout.EndVertical();
    }

    private void DrawOptionalProperty(
        SerializedProperty property,
        bool includeChildren = false,
        string customLabel = "")
    {
        if (property == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(customLabel))
        {
            EditorGUILayout.PropertyField(property, includeChildren);
        }
        else
        {
            EditorGUILayout.PropertyField(property, new GUIContent(customLabel), includeChildren);
        }
    }
}
#endif
