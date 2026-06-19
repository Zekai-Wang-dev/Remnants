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
        SerializedProperty equipSlotType = serializedObject.FindProperty("equipSlotType");

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
                EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(equipSlotType);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponData"), true);
                break;

            case ItemType.Equipment:
                EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(equipSlotType);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("equipmentData"), true);
                break;

            case ItemType.Consumable:
                EditorGUILayout.LabelField("Consumable", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("consumableData"), true);
                break;

            case ItemType.Quest:
                EditorGUILayout.LabelField("Quest Item", EditorStyles.boldLabel);
                break;

            case ItemType.Special:
                EditorGUILayout.LabelField("Special Item", EditorStyles.boldLabel);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}