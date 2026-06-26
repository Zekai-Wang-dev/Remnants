#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneData))]
public class SceneDataEditor : Editor
{
    private SerializedProperty sceneId;
    private SerializedProperty sceneName;
    private SerializedProperty actions;
    private SerializedProperty requiredSceneId;
    private SerializedProperty requiredAreaId;
    private SerializedProperty autoCollectRequiredActorIds;
    private SerializedProperty requiredActorIds;
    private SerializedProperty isPermanent;
    private SerializedProperty isActivated;
    private SerializedProperty endCommands;
    private SerializedProperty newQuest;

    private void OnEnable()
    {
        sceneId = serializedObject.FindProperty("sceneId");
        sceneName = serializedObject.FindProperty("sceneName");
        actions = serializedObject.FindProperty("actions");
        requiredSceneId = serializedObject.FindProperty("requiredSceneId");
        requiredAreaId = serializedObject.FindProperty("requiredAreaId");
        autoCollectRequiredActorIds = serializedObject.FindProperty("autoCollectRequiredActorIds");
        requiredActorIds = serializedObject.FindProperty("requiredActorIds");
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

        EditorGUILayout.LabelField("Playback Requirements", EditorStyles.boldLabel);
        DrawOptionalProperty(requiredSceneId);
        DrawOptionalProperty(requiredAreaId);
        DrawOptionalProperty(autoCollectRequiredActorIds);
        DrawOptionalProperty(requiredActorIds, true);

        EditorGUILayout.HelpBox("Leave Required Scene/Area blank for generic cinematics. For quest completion cinematics, fill them so queued scenes only play in the correct loaded area.", MessageType.Info);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Scene Actions", EditorStyles.boldLabel);
        DrawOptionalProperty(actions, true);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
        DrawOptionalProperty(isPermanent);
        DrawOptionalProperty(isActivated);

        if (isPermanent != null && isPermanent.boolValue && sceneId != null && string.IsNullOrWhiteSpace(sceneId.stringValue))
        {
            EditorGUILayout.HelpBox(
                "Permanent cinematics should have a stable sceneId so save/load can identify them.",
                MessageType.Warning
            );
        }

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
    private const float BoxPadding = 4f;

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

        SceneAction.ActionType actionType = GetActionType(type);

        EditorGUI.indentLevel++;

        switch (actionType)
        {
            case SceneAction.ActionType.None:
                DrawInfoBox(ref y, position, "No action-specific fields.");
                break;

            case SceneAction.ActionType.Move:
                DrawHeader(ref y, position, "Move Settings");
                DrawActorCommandList(ref y, position, actorCommands, actionType);
                DrawProperty(ref y, position, waitAfterAction);
                break;

            case SceneAction.ActionType.TurnAround:
                DrawHeader(ref y, position, "Sprite Flip Settings");
                DrawInfoBox(ref y, position, "Toggle flips the current sprite direction. SetValue is safer when a cinematic may reload, replay, or start from a known direction.");
                DrawActorCommandList(ref y, position, actorCommands, actionType);
                DrawProperty(ref y, position, waitAfterAction);
                break;

            case SceneAction.ActionType.Animation:
                DrawHeader(ref y, position, "Animation Settings");
                DrawActorCommandList(ref y, position, actorCommands, actionType);
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

        SceneAction.ActionType actionType = GetActionType(type);

        switch (actionType)
        {
            case SceneAction.ActionType.None:
                height += GetHelpBoxHeight();
                break;

            case SceneAction.ActionType.Move:
                height += GetHeaderHeight();
                height += GetActorCommandListHeight(actorCommands, actionType);
                height += GetPropertyHeight(waitAfterAction);
                break;

            case SceneAction.ActionType.TurnAround:
                height += GetHeaderHeight();
                height += GetHelpBoxHeight();
                height += GetActorCommandListHeight(actorCommands, actionType);
                height += GetPropertyHeight(waitAfterAction);
                break;

            case SceneAction.ActionType.Animation:
                height += GetHeaderHeight();
                height += GetActorCommandListHeight(actorCommands, actionType);
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

    private SceneAction.ActionType GetActionType(SerializedProperty type)
    {
        if (type == null)
        {
            return SceneAction.ActionType.None;
        }

        return (SceneAction.ActionType)type.enumValueIndex;
    }

    private string GetFoldoutLabel(
        SerializedProperty actionName,
        SerializedProperty type,
        GUIContent defaultLabel)
    {
        string nameText = actionName != null ? actionName.stringValue : "";
        string typeText = "Action";

        if (type != null && type.enumValueIndex >= 0 && type.enumValueIndex < type.enumDisplayNames.Length)
        {
            typeText = type.enumDisplayNames[type.enumValueIndex];
        }

        if (string.IsNullOrWhiteSpace(nameText))
        {
            return $"{defaultLabel.text} - {typeText}";
        }

        return $"{nameText} - {typeText}";
    }

    private void DrawActorCommandList(
        ref float y,
        Rect position,
        SerializedProperty actorCommands,
        SceneAction.ActionType actionType)
    {
        if (actorCommands == null)
        {
            return;
        }

        Rect foldoutRect = new Rect(
            position.x,
            y,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        actorCommands.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            actorCommands.isExpanded,
            $"Actor Commands ({actorCommands.arraySize})",
            true
        );

        y += EditorGUIUtility.singleLineHeight + Spacing;

        if (!actorCommands.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;

        SerializedProperty sizeProperty = actorCommands.FindPropertyRelative("Array.size");
        DrawProperty(ref y, position, sizeProperty, false, "Size");

        for (int i = 0; i < actorCommands.arraySize; i++)
        {
            SerializedProperty command = actorCommands.GetArrayElementAtIndex(i);
            DrawActorCommandElement(ref y, position, command, i, actionType);
        }

        EditorGUI.indentLevel--;
    }

    private float GetActorCommandListHeight(
        SerializedProperty actorCommands,
        SceneAction.ActionType actionType)
    {
        if (actorCommands == null)
        {
            return 0f;
        }

        float height = EditorGUIUtility.singleLineHeight + Spacing;

        if (!actorCommands.isExpanded)
        {
            return height;
        }

        SerializedProperty sizeProperty = actorCommands.FindPropertyRelative("Array.size");
        height += GetPropertyHeight(sizeProperty);

        for (int i = 0; i < actorCommands.arraySize; i++)
        {
            SerializedProperty command = actorCommands.GetArrayElementAtIndex(i);
            height += GetActorCommandElementHeight(command, actionType);
        }

        return height;
    }

    private void DrawActorCommandElement(
        ref float y,
        Rect position,
        SerializedProperty command,
        int index,
        SceneAction.ActionType actionType)
    {
        if (command == null)
        {
            return;
        }

        Rect boxRect = new Rect(
            position.x,
            y,
            position.width,
            GetActorCommandElementHeight(command, actionType) - Spacing
        );

        GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

        Rect foldoutRect = new Rect(
            position.x + BoxPadding,
            y + BoxPadding,
            position.width - BoxPadding * 2f,
            EditorGUIUtility.singleLineHeight
        );

        command.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            command.isExpanded,
            $"Command {index}",
            true
        );

        y += EditorGUIUtility.singleLineHeight + Spacing + BoxPadding;

        if (!command.isExpanded)
        {
            y += BoxPadding;
            return;
        }

        EditorGUI.indentLevel++;

        Rect childPosition = new Rect(
            position.x + BoxPadding,
            y,
            position.width - BoxPadding * 2f,
            position.height
        );

        DrawProperty(ref y, childPosition, command.FindPropertyRelative("actorId"));

        switch (actionType)
        {
            case SceneAction.ActionType.Move:
                DrawProperty(ref y, childPosition, command.FindPropertyRelative("moveDirection"));
                DrawProperty(ref y, childPosition, command.FindPropertyRelative("moveDistance"));
                DrawProperty(ref y, childPosition, command.FindPropertyRelative("moveSpeed"));
                break;

            case SceneAction.ActionType.TurnAround:
                SerializedProperty spriteFlipMode = command.FindPropertyRelative("spriteFlipMode");
                DrawProperty(ref y, childPosition, spriteFlipMode);

                if (spriteFlipMode != null &&
                    spriteFlipMode.enumValueIndex == (int)SceneActorCommand.SpriteFlipMode.SetValue)
                {
                    DrawProperty(ref y, childPosition, command.FindPropertyRelative("targetFlipX"));
                }
                break;

            case SceneAction.ActionType.Animation:
                DrawProperty(ref y, childPosition, command.FindPropertyRelative("animationClip"));
                break;

            default:
                EditorGUI.PropertyField(
                    new Rect(childPosition.x, y, childPosition.width, EditorGUIUtility.singleLineHeight),
                    command,
                    true
                );
                y += EditorGUI.GetPropertyHeight(command, true) + Spacing;
                break;
        }

        EditorGUI.indentLevel--;
        y += BoxPadding;
    }

    private float GetActorCommandElementHeight(
        SerializedProperty command,
        SceneAction.ActionType actionType)
    {
        if (command == null)
        {
            return 0f;
        }

        float height = EditorGUIUtility.singleLineHeight + Spacing + BoxPadding * 2f;

        if (!command.isExpanded)
        {
            return height;
        }

        height += GetPropertyHeight(command.FindPropertyRelative("actorId"));

        switch (actionType)
        {
            case SceneAction.ActionType.Move:
                height += GetPropertyHeight(command.FindPropertyRelative("moveDirection"));
                height += GetPropertyHeight(command.FindPropertyRelative("moveDistance"));
                height += GetPropertyHeight(command.FindPropertyRelative("moveSpeed"));
                break;

            case SceneAction.ActionType.TurnAround:
                SerializedProperty spriteFlipMode = command.FindPropertyRelative("spriteFlipMode");
                height += GetPropertyHeight(spriteFlipMode);

                if (spriteFlipMode != null &&
                    spriteFlipMode.enumValueIndex == (int)SceneActorCommand.SpriteFlipMode.SetValue)
                {
                    height += GetPropertyHeight(command.FindPropertyRelative("targetFlipX"));
                }
                break;

            case SceneAction.ActionType.Animation:
                height += GetPropertyHeight(command.FindPropertyRelative("animationClip"));
                break;

            default:
                height += GetPropertyHeight(command, true);
                break;
        }

        return height;
    }

    private void DrawProperty(
        ref float y,
        Rect position,
        SerializedProperty property,
        bool includeChildren = false,
        string customLabel = "")
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

        if (string.IsNullOrWhiteSpace(customLabel))
        {
            EditorGUI.PropertyField(rect, property, includeChildren);
        }
        else
        {
            EditorGUI.PropertyField(rect, property, new GUIContent(customLabel), includeChildren);
        }

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
