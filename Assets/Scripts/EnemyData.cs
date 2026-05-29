using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{

    public float maxHealth;
    public float speed;
    public float damage;
    public float trueDefense;
    public float accuracy;
    public float blockBypassRate;
    public float dodgeBypassRate;
    public float energyReserves;
    public float parryChance;
    public float blockChance;
    public float dodgeChance; 
    public GameObject enemyPrefab; // Reference to the enemy prefab for instantiations

}
