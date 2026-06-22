using TMPro;
using UnityEngine;

public class HoverStatRowUI : MonoBehaviour
{
    [Header("Stat Info")]
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text statBeforeText;
    [SerializeField] private TMP_Text statAfterText;

    public void Setup(string statName, string beforeText, string afterText)
    {
        if (statNameText != null)
        {
            statNameText.text = statName;
            statNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statName));
        }

        if (statBeforeText != null)
        {
            statBeforeText.text = beforeText;
            statBeforeText.gameObject.SetActive(!string.IsNullOrWhiteSpace(beforeText));
        }

        if (statAfterText != null)
        {
            statAfterText.text = afterText;
            statAfterText.gameObject.SetActive(!string.IsNullOrWhiteSpace(afterText));
        }
    }
}