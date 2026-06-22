using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HoverStatUI : MonoBehaviour
{
    [Header("Main Hover UI")]
    [SerializeField] private GameObject hoverContentRoot;
    [SerializeField] private Image hoverSlotImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescText;

    [Header("Spawned Stat Rows")]
    [SerializeField] private HoverStatRowUI statRowPrefab;
    [SerializeField] private Transform statRowParent;

    [Header("Colors")]
    [SerializeField] private string greaterColorHex = "#39FF14";
    [SerializeField] private string lowerColorHex = "#FF4D4D";
    [SerializeField] private string equalColorHex = "#FFFFFF";

    private void OnEnable()
    {
        InventorySlotUI.AnyItemHovered += UpdateHoverUI;
        InventorySlotUI.AnyItemHoverExited += HideHoverUI;

        HideHoverUI();
    }

    private void OnDisable()
    {
        InventorySlotUI.AnyItemHovered -= UpdateHoverUI;
        InventorySlotUI.AnyItemHoverExited -= HideHoverUI;
    }

    private void UpdateHoverUI(ItemData item, bool hoveredSlotEquipped)
    {
        ClearRows();

        if (item == null)
        {
            HideHoverUI();
            return;
        }

        if (hoverContentRoot != null)
        {
            hoverContentRoot.SetActive(true);
        }

        UpdateMainInfo(item);

        PlayerData playerData = GetPlayerData();

        if (playerData == null)
        {
            CreateRow("Info", "", "Player data missing");
            return;
        }

        int createdRows = CreateStatRows(item, playerData, hoveredSlotEquipped);

        if (createdRows == 0)
        {
            CreateRow("Info", "", "No stat changes");
        }
    }

    private void HideHoverUI()
    {
        ClearRows();
        ClearMainInfo();

        if (hoverContentRoot != null)
        {
            hoverContentRoot.SetActive(false);
        }
    }

    private PlayerData GetPlayerData()
    {
        if (PlayerDataManager.Instance == null)
        {
            return null;
        }

        return PlayerDataManager.Instance.playerData;
    }

    private void UpdateMainInfo(ItemData item)
    {
        if (hoverContentRoot != null)
        {
            hoverContentRoot.SetActive(true);
        }

        if (hoverSlotImage != null)
        {
            hoverSlotImage.gameObject.SetActive(true);

            if (item.imageIcon != null)
            {
                hoverSlotImage.enabled = true;
                hoverSlotImage.sprite = item.imageIcon;
                hoverSlotImage.color = Color.white;
            }
            else
            {
                hoverSlotImage.enabled = false;
                hoverSlotImage.sprite = null;
            }
        }

        if (itemNameText != null)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = item.itemName;
        }

        if (itemDescText != null)
        {
            itemDescText.gameObject.SetActive(true);
            itemDescText.text = item.description;
        }
    }

    private void ClearMainInfo()
    {
        if (hoverSlotImage != null)
        {
            hoverSlotImage.sprite = null;
            hoverSlotImage.enabled = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }

        if (itemDescText != null)
        {
            itemDescText.text = "";
            itemDescText.gameObject.SetActive(false);
        }
    }

    private int TryCreateConsumableHealthRow(
    string statName,
    float currentHealth,
    float maxHealth,
    float flatHealAmount,
    float maxHealthPercentage)
    {
        if (maxHealth <= 0f)
        {
            return 0;
        }

        float percentageHealAmount = maxHealth * maxHealthPercentage;
        float totalHealAmount = flatHealAmount + percentageHealAmount;

        if (totalHealAmount <= 0f)
        {
            return 0;
        }

        float afterValue = Mathf.Min(maxHealth, currentHealth + totalHealAmount);

        if (Mathf.Approximately(afterValue, currentHealth))
        {
            return 0;
        }

        string beforeText = FormatStat(currentHealth);
        string afterText = ColorAfterStat(afterValue, currentHealth);

        CreateRow(statName, beforeText, afterText);
        return 1;
    }

    private int CreateConsumableRows(ConsumableData consumableData, PlayerData playerData)
    {
        if (consumableData == null)
        {
            CreateRow("Info", "", "No consumable data");
            return 1;
        }

        int count = 0;

        count += TryCreateConsumableHealthRow(
            "Health",
            playerData.currentHealth,
            playerData.currentMaxHealth,
            consumableData.health,
            consumableData.healthPercentage
        );

        return count;
    }

    private int CreateStatRows(ItemData item, PlayerData playerData, bool hoveredSlotEquipped)
    {
        ItemData currentlyEquippedItem = GetCurrentlyEquippedItemInSameSlot(item, hoveredSlotEquipped);

        switch (item.itemType)
        {
            case ItemType.Weapon:
                return CreateWeaponRows(
                    item.weaponData,
                    currentlyEquippedItem != null ? currentlyEquippedItem.weaponData : null,
                    playerData,
                    hoveredSlotEquipped
                );

            case ItemType.Equipment:
                return CreateEquipmentRows(
                    item.equipmentData,
                    currentlyEquippedItem != null ? currentlyEquippedItem.equipmentData : null,
                    playerData,
                    hoveredSlotEquipped
                );

            case ItemType.Consumable:
                return CreateConsumableRows(item.consumableData, playerData);

            default:
                return 0;
        }
    }

    private ItemData GetCurrentlyEquippedItemInSameSlot(ItemData hoveredItem, bool hoveredSlotEquipped)
    {
        if (hoveredItem == null)
        {
            return null;
        }

        if (hoveredSlotEquipped)
        {
            return null;
        }

        if (hoveredItem.equipSlotType == EquipSlotType.None)
        {
            return null;
        }

        if (InventoryManager.Instance == null || InventoryManager.Instance.inventory == null)
        {
            return null;
        }

        foreach (InventorySlotData slot in InventoryManager.Instance.inventory)
        {
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                continue;
            }

            if (!slot.equipped)
            {
                continue;
            }

            if (slot.item.equipSlotType == hoveredItem.equipSlotType)
            {
                return slot.item;
            }
        }

        return null;
    }

    private int CreateWeaponRows(
        WeaponData hoveredWeaponData,
        WeaponData currentlyEquippedWeaponData,
        PlayerData playerData,
        bool hoveredSlotEquipped)
    {
        if (hoveredWeaponData == null)
        {
            CreateRow("Info", "", "No weapon data");
            return 1;
        }

        int count = 0;

        count += TryCreateStatRow("Damage", playerData.currentDamage, hoveredWeaponData.damage, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.damage : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Speed", playerData.currentSpeed, hoveredWeaponData.speed, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.speed : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Accuracy", playerData.currentAccuracy, hoveredWeaponData.accuracy, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.accuracy : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Block Bypass", playerData.currentBlockBypassRate, hoveredWeaponData.blockBypassRate, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.blockBypassRate : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Dodge Bypass", playerData.currentDodgeBypassRate, hoveredWeaponData.dodgeBypassRate, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.dodgeBypassRate : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Parry Chance", playerData.currentParryChance, hoveredWeaponData.parryChance, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.parryChance : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Block Chance", playerData.currentBlockChance, hoveredWeaponData.blockChance, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.blockChance : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Energy", playerData.currentEnergyReserves, hoveredWeaponData.energyReserves, currentlyEquippedWeaponData != null ? currentlyEquippedWeaponData.energyReserves : 0f, hoveredSlotEquipped);

        return count;
    }

    private int CreateEquipmentRows(
        EquipmentData hoveredEquipmentData,
        EquipmentData currentlyEquippedEquipmentData,
        PlayerData playerData,
        bool hoveredSlotEquipped)
    {
        if (hoveredEquipmentData == null)
        {
            CreateRow("Info", "", "No equipment data");
            return 1;
        }

        int count = 0;

        count += TryCreateStatRow("Max Health", playerData.currentMaxHealth, hoveredEquipmentData.maxHealth, currentlyEquippedEquipmentData != null ? currentlyEquippedEquipmentData.maxHealth : 0f, hoveredSlotEquipped); count += TryCreateStatRow("Speed", playerData.currentSpeed, hoveredEquipmentData.speed, currentlyEquippedEquipmentData != null ? currentlyEquippedEquipmentData.speed : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Base Defense", playerData.currentBaseDefense, hoveredEquipmentData.baseDefense, currentlyEquippedEquipmentData != null ? currentlyEquippedEquipmentData.baseDefense : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("True Defense", playerData.currentTrueDefense, hoveredEquipmentData.trueDefense, currentlyEquippedEquipmentData != null ? currentlyEquippedEquipmentData.trueDefense : 0f, hoveredSlotEquipped);
        count += TryCreateStatRow("Energy", playerData.currentEnergyReserves, hoveredEquipmentData.energyReserves, currentlyEquippedEquipmentData != null ? currentlyEquippedEquipmentData.energyReserves : 0f, hoveredSlotEquipped);

        return count;
    }

    private int TryCreateStatRow(
        string statName,
        float currentValue,
        float hoveredItemValue,
        float currentlyEquippedItemValue,
        bool hoveredSlotEquipped)
    {
        float statChange;

        if (hoveredSlotEquipped)
        {
            statChange = -hoveredItemValue;
        }
        else
        {
            statChange = hoveredItemValue - currentlyEquippedItemValue;
        }

        if (Mathf.Approximately(statChange, 0f))
        {
            return 0;
        }

        float afterValue = currentValue + statChange;

        string beforeText = FormatStat(currentValue);
        string afterText = ColorAfterStat(afterValue, currentValue);

        CreateRow(statName, beforeText, afterText);
        return 1;
    }

    private int TryCreateConsumableStatRow(string statName, float currentValue, float addedValue)
    {
        if (Mathf.Approximately(addedValue, 0f))
        {
            return 0;
        }

        float afterValue = currentValue + addedValue;

        string beforeText = FormatStat(currentValue);
        string afterText = ColorAfterStat(afterValue, currentValue);

        CreateRow(statName, beforeText, afterText);
        return 1;
    }

    private int TryCreateConsumablePercentageStatRow(string statName, float currentValue, float percentageValue)
    {
        if (Mathf.Approximately(percentageValue, 0f))
        {
            return 0;
        }

        float addedValue = currentValue * percentageValue;
        float afterValue = currentValue + addedValue;

        string beforeText = FormatStat(currentValue);
        string afterText = ColorAfterStat(afterValue, currentValue);

        CreateRow(statName, beforeText, afterText);
        return 1;
    }

    private void CreateRow(string statName, string beforeText, string afterText)
    {
        if (statRowPrefab == null || statRowParent == null)
        {
            Debug.LogWarning("HoverStatUI is missing statRowPrefab or statRowParent.");
            return;
        }

        HoverStatRowUI row = Instantiate(statRowPrefab, statRowParent);
        row.Setup(statName, beforeText, afterText);
    }

    private void ClearRows()
    {
        if (statRowParent == null)
        {
            return;
        }

        for (int i = statRowParent.childCount - 1; i >= 0; i--)
        {
            Destroy(statRowParent.GetChild(i).gameObject);
        }
    }

    private string ColorAfterStat(float afterValue, float beforeValue)
    {
        string colorHex = equalColorHex;

        if (afterValue > beforeValue)
        {
            colorHex = greaterColorHex;
        }
        else if (afterValue < beforeValue)
        {
            colorHex = lowerColorHex;
        }

        return $"<color={colorHex}>{FormatStat(afterValue)}</color>";
    }

    private string FormatStat(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }

        return value.ToString("0.##");
    }
}