using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private bool isOpen;

    private void Start()
    {
        if (inventoryRoot == null)
        {
            inventoryRoot = gameObject;
        }

        BuildGrid();
        Refresh();
        CloseInventory();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }
    }

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
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        isOpen = true;
        inventoryRoot.SetActive(true);
        Refresh();
    }

    public void CloseInventory()
    {
        isOpen = false;
        inventoryRoot.SetActive(false);
    }

    private void BuildGrid()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryUI could not find InventoryManager.");
            return;
        }

        if (slotParent == null)
        {
            slotParent = transform;
        }

        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        slotUIs.Clear();

        int slotCount = InventoryManager.Instance.InventorySize;

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, slotParent);

            InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();

            if (slotUI == null)
            {
                slotUI = slotObject.AddComponent<InventorySlotUI>();
            }

            slotUI.Setup(i);
            slotUIs.Add(slotUI);
        }
    }

    public void Refresh()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < slotUIs.Count; i++)
        {
            InventorySlotData slot = InventoryManager.Instance.GetSlot(i);
            slotUIs[i].SetSlot(slot);
        }
    }
}