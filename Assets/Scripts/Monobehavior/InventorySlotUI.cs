using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;

    private int slotIndex;

    public void Setup(int index)
    {
        slotIndex = index;
    }

    public void SetSlot(InventorySlotData slot)
    {
        if (iconImage == null)
        {
            return;
        }

        if (slot == null || slot.IsEmpty)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;

            if (quantityText != null)
            {
                quantityText.text = "";
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
    }
}