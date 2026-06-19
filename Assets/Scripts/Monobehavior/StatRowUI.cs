using TMPro;
using UnityEngine;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text statValueText;

    public void SetStat(string statName, float value, bool showPercent = false)
    {
        if (statNameText != null)
        {
            statNameText.text = statName;
        }

        if (statValueText != null)
        {
            statValueText.text = showPercent
                ? $"{value:0.#}%"
                : $"{value:0.#}";
        }
    }
}