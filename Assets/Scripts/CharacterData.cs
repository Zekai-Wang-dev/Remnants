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

}
