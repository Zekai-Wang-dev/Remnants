using TMPro;
using UnityEngine;

public class QuestRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    public void SetQuest(string questName, int value, int maxValue)
    {
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(questName)
                ? "Unnamed Quest"
                : questName;
        }

        if (valueText != null)
        {
            valueText.text = value + " / " + maxValue;
        }
    }
}