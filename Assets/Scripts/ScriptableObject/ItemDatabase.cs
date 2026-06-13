using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new();

    private Dictionary<string, ItemData> lookup;

    public void Initialize()
    {
        lookup = new Dictionary<string, ItemData>();

        foreach (ItemData item in items)
        {
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.itemId)) continue;

            if (!lookup.ContainsKey(item.itemId))
            {
                lookup.Add(item.itemId, item);
            }
            else
            {
                Debug.LogWarning($"Duplicate itemId found: {item.itemId}");
            }
        }
    }

    public ItemData GetItemById(string itemId)
    {
        if (lookup == null)
        {
            Initialize();
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        lookup.TryGetValue(itemId, out ItemData item);
        return item;
    }
}