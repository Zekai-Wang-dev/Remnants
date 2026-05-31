using System.Collections.Generic;
using UnityEngine;

public class CombatPreparation : MonoBehaviour
{
    public List<CharacterData> data;
    public List<Transform> t0gridLocations;
    public List<Transform> t1gridLocations;

    private CombatManager combatManager;
    private List<Character> t0Characters = new List<Character>();
    private List<Character> t1Characters = new List<Character>();

    public void combatPreparations()
    {

        GetCharacterData();

        foreach (CharacterData c in data)
        {
            if (c.charType == "Friendly")
            {
                // Instantiate the character prefab
                GameObject cPrefab = Instantiate(c.characterPrefab);
                cPrefab.transform.position = t0gridLocations[c.gridLocation].position;

                // Get the Character component from the instantiated prefab
                Character friendlyC = cPrefab.GetComponent<Character>();
                if (friendlyC == null)
                {
                    // If no Character component exists, add one
                    friendlyC = cPrefab.AddComponent<Character>();
                }

                // Set the character data
                friendlyC.data = c;
                friendlyC.isPlayer = c.isPlayer;
                friendlyC.team = 0; // Player team

                t0Characters.Add(friendlyC);
            }
            else if (c.charType == "Enemy")
            {
                // Instantiate the character prefab
                GameObject cPrefab = Instantiate(c.characterPrefab);
                cPrefab.transform.position = t1gridLocations[c.gridLocation].position;

                // Get the Character component from the instantiated prefab
                Character enemyC = cPrefab.GetComponent<Character>();
                if (enemyC == null)
                {
                    // If no Character component exists, add one
                    enemyC = cPrefab.AddComponent<Character>();
                }

                // Set the character data
                enemyC.data = c;
                enemyC.isPlayer = false;
                enemyC.team = 1; // Enemy team

                t1Characters.Add(enemyC);
            }
        }
    }

    public void GetCharacterData()
    {

        data.Clear();

        PlayerController playerController = PlayerController.Instance;
        PlayerDataManager playerDataManager = PlayerDataManager.Instance;

        List<RandomEnemyEntry> randomEnemyEntries = playerController.currentArea.randomEncounterPool;

        data.Add(playerDataManager.playerCharacterData);

        foreach (RandomEnemyEntry entry in randomEnemyEntries)
        {

            data.Add(entry.enemyData);

        }

    }

    void Start()
    {
        combatManager = CombatManager.Instance;
        combatPreparations();
        combatManager.StartCombat(t0Characters, t1Characters);
    }
}