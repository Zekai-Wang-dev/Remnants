using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform statRowParent;
    [SerializeField] private StatRowUI statRowPrefab;

    private readonly List<StatRowUI> statRows = new List<StatRowUI>();

    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerStatsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerStatsChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (PlayerDataManager.Instance == null)
        {
            return;
        }

        PlayerData data = PlayerDataManager.Instance.playerData;

        if (data == null)
        {
            return;
        }

        ClearRows();

        AddStat("Health", data.currentHealth);
        AddStat("Max Health", data.currentMaxHealth);
        AddStat("Speed", data.currentSpeed);
        AddStat("Damage", data.currentDamage);
        AddStat("Base Defense", data.currentBaseDefense);
        AddStat("True Defense", data.currentTrueDefense);
        AddStat("Accuracy", data.currentAccuracy, true);
        AddStat("Block Bypass", data.currentBlockBypassRate, true);
        AddStat("Dodge Bypass", data.currentDodgeBypassRate, true);
        AddStat("Energy Reserves", data.currentEnergyReserves);
        AddStat("Parry Chance", data.currentParryChance, true);
        AddStat("Block Chance", data.currentBlockChance, true);
        AddStat("Dodge Chance", data.currentDodgeChance, true);
    }

    private void AddStat(string statName, float value, bool showPercent = false)
    {
        if (statRowParent == null || statRowPrefab == null)
        {
            return;
        }

        StatRowUI row = Instantiate(statRowPrefab, statRowParent);
        row.SetStat(statName, value, showPercent);
        statRows.Add(row);
    }

    private void ClearRows()
    {
        for (int i = 0; i < statRows.Count; i++)
        {
            if (statRows[i] != null)
            {
                Destroy(statRows[i].gameObject);
            }
        }

        statRows.Clear();
    }
}