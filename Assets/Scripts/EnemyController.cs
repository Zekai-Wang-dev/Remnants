using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private EnemyData enemyData;

    public void Initialize(EnemyData data)
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