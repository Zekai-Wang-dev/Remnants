using System.Collections.Generic;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryRoot;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.U;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private bool isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isOpen)
        {
            CloseEquipment();
        }
        else
        {
            OpenEquipment();
        }
    }

    public void OpenEquipment()
    {

        inventoryRoot.SetActive(true);
        isOpen = true;
    }

    public void CloseEquipment()
    {

        inventoryRoot.SetActive(false);
        isOpen = false;
    }

}
