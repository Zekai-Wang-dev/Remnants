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

        EditorGUILayout.PropertyField(itemName);
        EditorGUILayout.PropertyField(itemType);

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