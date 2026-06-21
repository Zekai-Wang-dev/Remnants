#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneData))]
public class SceneDataEditor : Editor
{
    private SerializedProperty sceneId;
    private SerializedProperty sceneName;
    private SerializedProperty actions;
    private SerializedProperty isPermanent;
    private SerializedProperty isActivated;
    private SerializedProperty endCommands;
    private SerializedProperty newQuest;

    private void OnEnable()
    {
        sceneId = serializedObject.FindProperty("sceneId");
        sceneName = serializedObject.FindProperty("sceneName");
        actions = serializedObject.FindProperty("actions");
        isPermanent = serializedObject.FindProperty("isPermanent");
        isActivated = serializedObject.FindProperty("isActivated");
        endCommands = serializedObject.FindProperty("endCommands");
        newQuest = serializedObject.FindProperty("newQuest");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Scene Info", EditorStyles.boldLabel);
        DrawOptionalProperty(sceneId);
        DrawOptionalProperty(sceneName);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Scene Actions", EditorStyles.boldLabel);
        DrawOptionalProperty(actions, true);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
        DrawOptionalProperty(isPermanent);
        DrawOptionalProperty(isActivated);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("After Scene Ends", EditorStyles.boldLabel);
        DrawOptionalProperty(endCommands, true);
        DrawOptionalProperty(newQuest, true);

        serializedObject.ApplyModifiedProperties();
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

[CustomPropertyDrawer(typeof(SceneAction))]
public class SceneActionDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty actionName = property.FindPropertyRelative("actionName");
        SerializedProperty type = property.FindPropertyRelative("type");
        SerializedProperty actorCommands = property.FindPropertyRelative("actorCommands");
        SerializedProperty dialogueBeats = property.FindPropertyRelative("dialogueBeats");
        SerializedProperty waitAfterAction = property.FindPropertyRelative("waitAfterAction");

        Rect foldoutRect = new Rect(
            position.x,
            position.y,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        string foldoutLabel = GetFoldoutLabel(actionName, type, label);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = foldoutRect.yMax + Spacing;

        DrawProperty(ref y, position, actionName);
        DrawProperty(ref y, position, type);

        SceneAction.ActionType actionType = SceneAction.ActionType.None;

        if (type != null)
        {
            actionType = (SceneAction.ActionType)type.enumValueIndex;
        }

        EditorGUI.indentLevel++;

        switch (actionType)
        {
            case SceneAction.ActionType.None:
                DrawInfoBox(ref y, position, "No action-specific fields.");
                break;

            case SceneAction.ActionType.Move:
                DrawHeader(ref y, position, "Move Settings");
                DrawProperty(ref y, position, actorCommands, true);
                DrawProperty(ref y, position, waitAfterAction);
                break;

            case SceneAction.ActionType.Animation:
                DrawHeader(ref y, position, "Animation Settings");
                DrawProperty(ref y, position, actorCommands, true);
                DrawProperty(ref y, position, waitAfterAction);
                break;

            case SceneAction.ActionType.Dialogue:
                DrawHeader(ref y, position, "Dialogue Settings");
                DrawProperty(ref y, position, dialogueBeats, true);
                DrawProperty(ref y, position, waitAfterAction);
                break;

            case SceneAction.ActionType.Wait:
                DrawHeader(ref y, position, "Wait Settings");
                DrawProperty(ref y, position, waitAfterAction);
                break;
        }

        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float height = EditorGUIUtility.singleLineHeight + Spacing;

        SerializedProperty actionName = property.FindPropertyRelative("actionName");
        SerializedProperty type = property.FindPropertyRelative("type");
        SerializedProperty actorCommands = property.FindPropertyRelative("actorCommands");
        SerializedProperty dialogueBeats = property.FindPropertyRelative("dialogueBeats");
        SerializedProperty waitAfterAction = property.FindPropertyRelative("waitAfterAction");

        height += GetPropertyHeight(actionName);
        height += GetPropertyHeight(type);

        SceneAction.ActionType actionType = SceneAction.ActionType.None;

        if (type != null)
        {
            actionType = (SceneAction.ActionType)type.enumValueIndex;
        }

        switch (actionType)
        {
            case SceneAction.ActionType.None:
                height += GetHelpBoxHeight();
                break;

            case SceneAction.ActionType.Move:
                height += GetHeaderHeight();
                height += GetPropertyHeight(actorCommands, true);
                height += GetPropertyHeight(waitAfterAction);
                break;

            case SceneAction.ActionType.Animation:
                height += GetHeaderHeight();
                height += GetPropertyHeight(actorCommands, true);
                height += GetPropertyHeight(waitAfterAction);
                break;

            case SceneAction.ActionType.Dialogue:
                height += GetHeaderHeight();
                height += GetPropertyHeight(dialogueBeats, true);
                height += GetPropertyHeight(waitAfterAction);
                break;

            case SceneAction.ActionType.Wait:
                height += GetHeaderHeight();
                height += GetPropertyHeight(waitAfterAction);
                break;
        }

        return height;
    }

    private string GetFoldoutLabel(
        SerializedProperty actionName,
        SerializedProperty type,
        GUIContent defaultLabel)
    {
        string nameText = "";

        if (actionName != null)
        {
            nameText = actionName.stringValue;
        }

        string typeText = "Action";

        if (type != null)
        {
            typeText = type.enumDisplayNames[type.enumValueIndex];
        }

        if (string.IsNullOrWhiteSpace(nameText))
        {
            return $"{defaultLabel.text} - {typeText}";
        }

        return $"{nameText} - {typeText}";
    }

    private void DrawProperty(
        ref float y,
        Rect position,
        SerializedProperty property,
        bool includeChildren = false)
    {
        if (property == null)
        {
            return;
        }

        float height = EditorGUI.GetPropertyHeight(property, includeChildren);

        Rect rect = new Rect(
            position.x,
            y,
            position.width,
            height
        );

        EditorGUI.PropertyField(rect, property, includeChildren);

        y += height + Spacing;
    }

    private float GetPropertyHeight(
        SerializedProperty property,
        bool includeChildren = false)
    {
        if (property == null)
        {
            return 0f;
        }

        return EditorGUI.GetPropertyHeight(property, includeChildren) + Spacing;
    }

    private void DrawHeader(ref float y, Rect position, string text)
    {
        Rect rect = new Rect(
            position.x,
            y,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        EditorGUI.LabelField(rect, text, EditorStyles.boldLabel);

        y += EditorGUIUtility.singleLineHeight + Spacing;
    }

    private float GetHeaderHeight()
    {
        return EditorGUIUtility.singleLineHeight + Spacing;
    }

    private void DrawInfoBox(ref float y, Rect position, string text)
    {
        float height = GetHelpBoxHeight() - Spacing;

        Rect rect = new Rect(
            position.x,
            y,
            position.width,
            height
        );

        EditorGUI.HelpBox(rect, text, MessageType.Info);

        y += height + Spacing;
    }

    private float GetHelpBoxHeight()
    {
        return EditorGUIUtility.singleLineHeight * 2f + Spacing;
    }
}
#endif