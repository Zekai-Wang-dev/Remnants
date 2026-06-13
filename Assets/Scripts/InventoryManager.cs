using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }

    public List<ItemData> inventory = new List<ItemData>();

    public int width = 5;
    public int height = 6; 

    public void InitializeInventory()
    {

        int inventorySize = width * height;

        for (int i = 0; i < inventorySize; i++)
        {

            inventory.Add(null); 

        }

    }

    public List<ItemData> GetInventory()
    {
        
        return inventory; 
    }


    
}
