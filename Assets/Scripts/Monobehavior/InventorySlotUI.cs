using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class ItemDataEvent : UnityEvent<ItemData> { }

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static event System.Action<ItemData, bool> AnyItemHovered;
    public static event System.Action AnyItemHoverExited;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image equipIconImage;

    [Header("Events")]
    [SerializeField] private ItemDataEvent onItemHovered = new ItemDataEvent();

    public event Action<ItemData> ItemHovered;

    private int slotIndex;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnSlotClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnSlotClicked);
        }
    }

    public void Setup(int index)
    {
        slotIndex = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        DeleteSlotItem();
    }

    private void DeleteSlotItem()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        InventorySlotData slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            return;
        }

        InventoryManager.Instance.ClearSlot(slotIndex);

        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.RequestSave();
        }

        AnyItemHoverExited?.Invoke();

        Debug.Log($"Deleted item from inventory slot {slotIndex}.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        InventorySlotData slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            return;
        }

        ItemHovered?.Invoke(slot.item);
        onItemHovered?.Invoke(slot.item);

        AnyItemHovered?.Invoke(slot.item, slot.equipped);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnyItemHoverExited?.Invoke();
    }

    private ItemData GetCurrentItemData()
    {
        if (InventoryManager.Instance == null)
        {
            return null;
        }

        InventorySlotData slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            return null;
        }

        return slot.item;
    }

    public void OnSlotClicked()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        InventorySlotData slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            return;
        }

        ItemData clickedItem = slot.item;

        if (InventoryManager.IsConsumableItem(clickedItem))
        {
            bool used = InventoryManager.Instance.UseConsumableSlot(slotIndex);

            if (used)
            {
                RefreshHoverAfterSlotChanged();
            }

            return;
        }

        if (!InventoryManager.IsEquippableItem(clickedItem))
        {
            return;
        }

        if (slot.equipped)
        {
            InventoryManager.Instance.UnequipSlot(slotIndex);
        }
        else
        {
            InventoryManager.Instance.EquipSlot(slotIndex);
        }

        RefreshHoverAfterSlotChanged();
    }

    private void RefreshHoverAfterSlotChanged()
    {
        if (InventoryManager.Instance == null)
        {
            AnyItemHoverExited?.Invoke();
            return;
        }

        InventorySlotData updatedSlot = InventoryManager.Instance.GetSlot(slotIndex);

        if (updatedSlot == null || updatedSlot.IsEmpty || updatedSlot.item == null)
        {
            AnyItemHoverExited?.Invoke();
            return;
        }

        AnyItemHovered?.Invoke(updatedSlot.item, updatedSlot.equipped);
    }

    public void SetSlot(InventorySlotData slot)
    {
        if (iconImage == null)
        {
            return;
        }

        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;

            if (quantityText != null)
            {
                quantityText.text = "";
            }

            if (equipIconImage != null)
            {
                equipIconImage.enabled = false;
            }

            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = slot.item.imageIcon;

        if (quantityText != null)
        {
            if (slot.item.isStackable && slot.quantity > 1)
            {
                quantityText.text = slot.quantity.ToString();
            }
            else
            {
                quantityText.text = "";
            }
        }

        if (equipIconImage != null)
        {
            equipIconImage.enabled = slot.equipped;
        }
    }
}