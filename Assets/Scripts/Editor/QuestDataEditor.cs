#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestData))]
public class QuestDataEditor : Editor
{
    private SerializedProperty questId;
    private SerializedProperty questName;
    private SerializedProperty questType;
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

        EditorGUILayout.LabelField("Quest Info", EditorStyles.boldLabel);

        DrawOptionalProperty(questId);
        DrawOptionalProperty(questName);
        DrawOptionalProperty(questType);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Requirement", EditorStyles.boldLabel);
        DrawOptionalProperty(questRequirementType);

        if (questRequirementType != null)
        {
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

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
        DrawOptionalProperty(questRewards, true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRequirementBox(string title, SerializedProperty property)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        DrawOptionalProperty(property, true);

        EditorGUILayout.EndVertical();
    }

    private void DrawOptionalProperty(SerializedProperty property, bool includeChildren = false)
    {
        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(property, includeChildren);
    }
}
#endif