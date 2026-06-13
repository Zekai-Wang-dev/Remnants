using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty itemName = serializedObject.FindProperty("itemName");
        SerializedProperty itemType = serializedObject.FindProperty("itemType");
        SerializedProperty itemId = serializedObject.FindProperty("itemId");
        SerializedProperty description = serializedObject.FindProperty("description");
        SerializedProperty imageIcon = serializedObject.FindProperty("imageIcon");
        SerializedProperty isStackable = serializedObject.FindProperty("isStackable");
        SerializedProperty maxStackSize = serializedObject.FindProperty("maxStackSize");

        EditorGUILayout.PropertyField(itemName);
        EditorGUILayout.PropertyField(itemType);
        EditorGUILayout.PropertyField(itemId);
        EditorGUILayout.PropertyField(description);
        EditorGUILayout.PropertyField(imageIcon);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isStackable);

        if (isStackable.boolValue)
        {
            EditorGUILayout.PropertyField(maxStackSize);
        }

        ItemType type = (ItemType)itemType.enumValueIndex;

        EditorGUILayout.Space();

        switch (type)
        {
            case ItemType.Weapon:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponData"), true);
                break;

            case ItemType.Equipment:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("equipmentData"), true);
                break;

            case ItemType.Consumable:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("consumableData"), true);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}