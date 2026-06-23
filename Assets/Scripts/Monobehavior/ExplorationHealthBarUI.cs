using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplorationHealthBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image healthBarImage;
    [SerializeField] private TMP_Text healthText;

    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerStatsChanged += UpdateHealthBar;
        }

        UpdateHealthBar();
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerStatsChanged -= UpdateHealthBar;
        }
    }

    public void UpdateHealthBar()
    {
        if (PlayerDataManager.Instance == null)
        {
            return;
        }

        PlayerData playerData = PlayerDataManager.Instance.playerData;

        if (playerData == null)
        {
            return;
        }

        float currentHealth = Mathf.Max(0f, playerData.currentHealth);
        float maxHealth = Mathf.Max(0f, playerData.currentMaxHealth);

        float healthPercentage = 0f;

        if (maxHealth > 0f)
        {
            healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
        }

        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = healthPercentage;
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {FormatHealth(currentHealth)} / {FormatHealth(maxHealth)}";
        }
    }

    private string FormatHealth(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }

        return value.ToString("0.#");
    }
}