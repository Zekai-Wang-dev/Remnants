using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    public ItemType itemType;
    public string itemId;
    public string itemName;
    public string description;
    public Sprite imageIcon;

    [Header("Stacking")]
    public bool isStackable = false;
    public int maxStackSize = 1;

    [Header("Equip Slot")]
    public EquipSlotType equipSlotType = EquipSlotType.None;

    public WeaponData weaponData;
    public EquipmentData equipmentData;
    public ConsumableData consumableData;
}

public enum ItemType
{
    Weapon,
    Equipment,
    Consumable,
    Quest,
    Special
}

public enum EquipSlotType
{
    None,
    Weapon,
    Head,
    Body,
    Legs,
    Hands,
    Accessory
}

[System.Serializable]
public class EquipmentData
{
    public float speed;
    public float maxHealth;
    public float baseDefense;
    public float trueDefense;
    public float energyReserves;
}

[System.Serializable]
public class WeaponData
{
    public float damage;
    public float speed;
    public float accuracy;
    public float blockBypassRate;
    public float dodgeBypassRate;
    public float parryChance;
    public float blockChance;
    public float energyReserves;
}

[System.Serializable]
public class ConsumableData
{
    public float health;
    public float healthPercentage;
    public float speed;
    public float speedPercentage;
    public float damage;
    public float damagePercentage;
    public float defense;
    public float defensePercentage;
    public float trueDefense;
    public float accuracy;
    public float blockBypassRate;
    public float dodgeBypassRate;
    public float energyReserves;
    public float parryChance;
    public float blockChance;
    public float dodgeChance;
}