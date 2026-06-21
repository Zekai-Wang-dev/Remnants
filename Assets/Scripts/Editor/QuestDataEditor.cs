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
    private SerializedProperty newQuest;
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
        newQuest = serializedObject.FindProperty("newQuest");
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
        DrawChainSection();
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

    private void DrawChainSection()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Quest Chain", EditorStyles.boldLabel);

        DrawOptionalProperty(isChained);

        if (isChained != null && isChained.boolValue)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawOptionalProperty(newQuest, false, "New Quest On Completion");
            DrawOptionalProperty(newQuestAutoClaimRewards, false, "New Quest Auto Claim Rewards");

            if (newQuest != null && newQuest.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "isChained is enabled, but no new quest is assigned.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.EndVertical();
        }
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