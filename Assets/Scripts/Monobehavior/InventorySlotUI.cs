using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image equipIconImage;

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

        if (!InventoryManager.IsEquippableItem(slot.item))
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