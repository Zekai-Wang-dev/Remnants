using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySaveData
{
    public List<InventorySlotSaveData> slots = new();
    public List<string> collectedOnceOnlyDropIds = new();
}

[System.Serializable]
public class InventorySlotSaveData
{
    public string itemId;
    public int quantity;
}

[System.Serializable]
public class InventorySlotData
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public event Action OnInventoryChanged;

    [Header("Inventory Size")]
    public int width = 5;
    public int height = 6;

    [Header("Runtime Inventory Data")]
    public List<InventorySlotData> inventory = new List<InventorySlotData>();

    private readonly HashSet<string> collectedOnceOnlyDropIds = new();

    public int InventorySize => width * height;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeInventoryData();
        LoadCollectedOnceOnlyDrops();
    }

    private void InitializeInventoryData()
    {
        int inventorySize = InventorySize;

        while (inventory.Count < inventorySize)
        {
            inventory.Add(new InventorySlotData());
        }

        while (inventory.Count > inventorySize)
        {
            inventory.RemoveAt(inventory.Count - 1);
        }

        NotifyInventoryChanged();
    }

    public InventorySlotData GetSlot(int index)
    {
        if (index < 0 || index >= inventory.Count)
        {
            return null;
        }

        return inventory[index];
    }

    public ItemData GetItem(int index)
    {
        InventorySlotData slot = GetSlot(index);
        return slot != null ? slot.item : null;
    }

    public int GetQuantity(int index)
    {
        InventorySlotData slot = GetSlot(index);
        return slot != null ? slot.quantity : 0;
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null)
        {
            Debug.LogWarning("Tried to add null item.");
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning("Tried to add item with amount <= 0.");
            return false;
        }

        int remainingAmount = amount;

        if (item.isStackable)
        {
            remainingAmount = AddToExistingStacks(item, remainingAmount);
        }

        remainingAmount = AddToEmptySlots(item, remainingAmount);

        NotifyInventoryChanged();

        if (remainingAmount > 0)
        {
            Debug.LogWarning($"Inventory full. Could not add {remainingAmount}x {item.itemName}.");
            return false;
        }

        return true;
    }

    private int AddToExistingStacks(ItemData item, int amount)
    {
        int maxStack = Mathf.Max(1, item.maxStackSize);

        for (int i = 0; i < inventory.Count; i++)
        {
            InventorySlotData slot = inventory[i];

            if (slot == null || slot.IsEmpty)
            {
                continue;
            }

            if (slot.item != item)
            {
                continue;
            }

            if (slot.quantity >= maxStack)
            {
                continue;
            }

            int availableSpace = maxStack - slot.quantity;
            int amountToAdd = Mathf.Min(availableSpace, amount);

            slot.quantity += amountToAdd;
            amount -= amountToAdd;

            if (amount <= 0)
            {
                return 0;
            }
        }

        return amount;
    }

    private int AddToEmptySlots(ItemData item, int amount)
    {
        int maxStack = item.isStackable ? Mathf.Max(1, item.maxStackSize) : 1;

        for (int i = 0; i < inventory.Count; i++)
        {
            InventorySlotData slot = inventory[i];

            if (slot == null)
            {
                slot = new InventorySlotData();
                inventory[i] = slot;
            }

            if (!slot.IsEmpty)
            {
                continue;
            }

            int amountToAdd = Mathf.Min(maxStack, amount);

            slot.item = item;
            slot.quantity = amountToAdd;

            amount -= amountToAdd;

            if (amount <= 0)
            {
                return 0;
            }
        }

        return amount;
    }

    public void SetItem(int index, ItemData item, int quantity = 1)
    {
        InventorySlotData slot = GetSlot(index);

        if (slot == null)
        {
            Debug.LogWarning($"Tried to set item at invalid index {index}.");
            return;
        }

        if (item == null || quantity <= 0)
        {
            slot.Clear();
        }
        else
        {
            slot.item = item;
            slot.quantity = item.isStackable
                ? Mathf.Clamp(quantity, 1, Mathf.Max(1, item.maxStackSize))
                : 1;
        }

        NotifyInventoryChanged();
    }

    public void RemoveItem(int index, int amount = 1)
    {
        InventorySlotData slot = GetSlot(index);

        if (slot == null || slot.IsEmpty)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        slot.quantity -= amount;

        if (slot.quantity <= 0)
        {
            slot.Clear();
        }

        NotifyInventoryChanged();
    }

    public void ClearSlot(int index)
    {
        InventorySlotData slot = GetSlot(index);

        if (slot == null)
        {
            return;
        }

        slot.Clear();
        NotifyInventoryChanged();
    }

    public bool HasCollectedOnceOnlyDrop(string dropId)
    {
        if (string.IsNullOrWhiteSpace(dropId))
        {
            return false;
        }

        return collectedOnceOnlyDropIds.Contains(dropId);
    }

    public void MarkOnceOnlyDropCollected(string dropId)
    {
        if (string.IsNullOrWhiteSpace(dropId))
        {
            return;
        }

        if (collectedOnceOnlyDropIds.Add(dropId))
        {
            SaveCollectedOnceOnlyDrops();
        }
    }

    private void SaveCollectedOnceOnlyDrops()
    {
        string saveString = string.Join(",", collectedOnceOnlyDropIds);
        PlayerPrefs.SetString("CollectedOnceOnlyDrops", saveString);
        PlayerPrefs.Save();
    }

    private void LoadCollectedOnceOnlyDrops()
    {
        collectedOnceOnlyDropIds.Clear();

        string saveString = PlayerPrefs.GetString("CollectedOnceOnlyDrops", "");

        if (string.IsNullOrWhiteSpace(saveString))
        {
            return;
        }

        string[] ids = saveString.Split(',');

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                collectedOnceOnlyDropIds.Add(id);
            }
        }
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public InventorySaveData CreateSaveData()
    {
        InventorySaveData saveData = new InventorySaveData();

        foreach (InventorySlotData slot in inventory)
        {
            InventorySlotSaveData slotSave = new InventorySlotSaveData();

            if (slot != null && !slot.IsEmpty && slot.item != null)
            {
                slotSave.itemId = slot.item.itemId;
                slotSave.quantity = slot.quantity;
            }
            else
            {
                slotSave.itemId = "";
                slotSave.quantity = 0;
            }

            saveData.slots.Add(slotSave);
        }

        saveData.collectedOnceOnlyDropIds = new List<string>(collectedOnceOnlyDropIds);

        return saveData;
    }

    public void LoadFromSaveData(InventorySaveData saveData, ItemDatabase itemDatabase)
    {
        if (saveData == null || itemDatabase == null)
        {
            return;
        }

        inventory.Clear();

        for (int i = 0; i < InventorySize; i++)
        {
            InventorySlotData slot = new InventorySlotData();

            if (i < saveData.slots.Count)
            {
                InventorySlotSaveData slotSave = saveData.slots[i];

                if (slotSave != null && !string.IsNullOrWhiteSpace(slotSave.itemId))
                {
                    ItemData item = itemDatabase.GetItemById(slotSave.itemId);

                    if (item != null)
                    {
                        slot.item = item;
                        slot.quantity = Mathf.Max(1, slotSave.quantity);
                    }
                }
            }

            inventory.Add(slot);
        }

        collectedOnceOnlyDropIds.Clear();

        if (saveData.collectedOnceOnlyDropIds != null)
        {
            foreach (string id in saveData.collectedOnceOnlyDropIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    collectedOnceOnlyDropIds.Add(id);
                }
            }
        }

        NotifyInventoryChanged();
    }

}