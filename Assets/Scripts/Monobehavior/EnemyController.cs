using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private CharacterData enemyData;

    public void Initialize(CharacterData data)
    {
        enemyData = data;
        // Add any initialization logic here
        // Example: Set health, speed, etc. based on enemyData
    }

    // Update is called once per frame
    void Update()
    {
        // Enemy AI logic here
    }
}