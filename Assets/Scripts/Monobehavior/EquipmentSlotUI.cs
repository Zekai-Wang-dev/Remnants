using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    private InventoryManager inventoryManager;

    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Equipment Slot")]
    [SerializeField] private EquipSlotType equipSlotType;

    private void OnEnable()
    {
        inventoryManager = InventoryManager.Instance;

        if (inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged += HandleEquipmentChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void HandleEquipmentChanged(EquipSlotType changedType)
    {
        if (changedType != equipSlotType)
        {
            return;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (iconImage == null)
        {
            Debug.LogWarning($"{name}: iconImage is not assigned.");
            return;
        }

        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning($"{name}: InventoryManager is null.");
            iconImage.sprite = null;
            iconImage.enabled = false;
            return;
        }

        if (equipSlotType == EquipSlotType.None)
        {
            Debug.LogWarning($"{name}: equipSlotType is None.");
            iconImage.sprite = null;
            iconImage.enabled = false;
            return;
        }

        InventorySlotData equippedSlot = inventoryManager.GetEquippedSlot(equipSlotType);

        if (equippedSlot == null || equippedSlot.IsEmpty || equippedSlot.item == null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            return;
        }

        iconImage.sprite = equippedSlot.item.imageIcon;
        iconImage.enabled = true;
        iconImage.gameObject.SetActive(true);

        Debug.Log($"{name}: sprite = {iconImage.sprite}, enabled = {iconImage.enabled}, active = {iconImage.gameObject.activeInHierarchy}");
    }
}