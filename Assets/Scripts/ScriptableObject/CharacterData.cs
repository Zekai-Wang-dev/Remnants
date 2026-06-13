using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string charType;
    public bool isPlayer;

    public float maxHealth;
    public float speed;
    public float damage;
    public float defense;
    public float trueDefense;
    public float accuracy;
    public float blockBypassRate;
    public float dodgeBypassRate;
    public float energyReserves;
    public float parryChance;
    public float blockChance;
    public float dodgeChance;

    public float attackDuration;

    public int gridLocation;

    public GameObject characterPrefab;

    [Header("Drops")]
    public List<ItemDrop> dropsPool = new();
}

[System.Serializable]
public class ItemDrop
{
    public ItemData item;

    [Range(0f, 1f)]
    public float dropChance = 1f;

    [Tooltip("If true, this item always drops when the enemy is defeated.")]
    public bool isPermanentDrop;

    [Header("Once-Only Drop")]
    [Tooltip("If true, this drop can only be received once across the save.")]
    public bool dropOnceOnly;

    [Tooltip("Optional. If empty, item.itemId will be used.")]
    public string uniqueDropId;

    [Header("Quantity")]
    public int minQuantity = 1;
    public int maxQuantity = 1;
}