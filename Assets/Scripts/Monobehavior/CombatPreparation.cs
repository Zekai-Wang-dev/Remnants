using System.Collections.Generic;
using UnityEngine;

public class CombatPreparation : MonoBehaviour
{
    public List<CharacterData> data = new();
    public List<Transform> t0gridLocations;
    public List<Transform> t1gridLocations;

    [Header("Health Bar")]
    public GameObject healthBarPrefab;

    private CombatManager combatManager;
    private readonly List<Character> t0Characters = new();
    private readonly List<Character> t1Characters = new();

    void Start()
    {
        combatManager = CombatManager.Instance;

        if (combatManager == null)
        {
            Debug.LogError("CombatManager not found.");
            return;
        }

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager is missing, so CombatPreparation cannot start combat.");
            enabled = false;
            return;
        }

        if (!spawnManager.IsCurrentSceneCombat)
        {
            Debug.LogWarning("CombatPreparation exists in a non-combat scene. It will not start combat.");
            enabled = false;
            return;
        }

        bool hasCustomEnemyPool = spawnManager.HasPendingCustomCombatEnemyPool();
        bool hasEncounterId = !string.IsNullOrWhiteSpace(spawnManager.currentEncounterId);

        if (!hasCustomEnemyPool && !hasEncounterId)
        {
            Debug.LogWarning("CombatPreparation found no pending custom enemy pool and no current encounter ID. Combat will not start.");
            enabled = false;
            return;
        }

        PrepareCombatants();

        if (t0Characters.Count == 0)
        {
            Debug.LogError("CombatPreparation created no player-side combatants. Combat will not start.");
            enabled = false;
            return;
        }

        if (t1Characters.Count == 0)
        {
            Debug.LogError("CombatPreparation created no enemy-side combatants. Combat will not start. Check currentEncounterId/currentEncounterAreaId or custom combat enemies.");
            enabled = false;
            return;
        }

        combatManager.StartCombat(t0Characters, t1Characters);
    }

    public void PrepareCombatants()
    {
        GetCharacterData();

        t0Characters.Clear();
        t1Characters.Clear();

        foreach (CharacterData c in data)
        {
            if (c == null)
            {
                Debug.LogWarning("Null CharacterData found during combat preparation.");
                continue;
            }

            if (c.characterPrefab == null)
            {
                Debug.LogWarning($"CharacterData '{c.name}' has no prefab assigned.");
                continue;
            }

            if (c.charType == "Friendly")
            {
                SpawnCombatCharacter(c, t0gridLocations, t0Characters, 0, c.isPlayer);
            }
            else if (c.charType == "Enemy")
            {
                SpawnCombatCharacter(c, t1gridLocations, t1Characters, 1, false);
            }
            else
            {
                Debug.LogWarning($"CharacterData '{c.name}' has unknown charType '{c.charType}'. Expected 'Friendly' or 'Enemy'.");
            }
        }
    }

    private void SpawnCombatCharacter(
        CharacterData characterData,
        List<Transform> gridLocations,
        List<Character> teamList,
        int team,
        bool isPlayer)
    {
        if (gridLocations == null || gridLocations.Count == 0)
        {
            Debug.LogError($"No grid locations assigned for team {team}.");
            return;
        }

        int gridIndex = Mathf.Clamp(characterData.gridLocation, 0, gridLocations.Count - 1);
        Transform spawnTransform = gridLocations[gridIndex];

        if (spawnTransform == null)
        {
            Debug.LogError($"Grid location {gridIndex} for team {team} is null.");
            return;
        }

        GameObject prefabInstance = Instantiate(characterData.characterPrefab, spawnTransform.position, Quaternion.identity);

        Character character = prefabInstance.GetComponent<Character>();
        if (character == null)
        {
            character = prefabInstance.AddComponent<Character>();
        }

        character.data = characterData;
        character.isPlayer = isPlayer;
        character.team = team;
        character.gridLocation = characterData.gridLocation;
        character.state = "Alive";
        character.ResetHealth();

        SpawnHealthBarFor(character, prefabInstance.transform);

        teamList.Add(character);
    }

    private void SpawnHealthBarFor(Character character, Transform targetTransform)
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("No healthBarPrefab assigned in CombatPreparation.");
            return;
        }

        GameObject healthBarObject = Instantiate(
            healthBarPrefab,
            Camera.main.WorldToScreenPoint(targetTransform.position + new Vector3(0f, 1.2f, 0f)),
            Quaternion.identity
        );

        healthBarObject.transform.SetParent(GameObject.FindFirstObjectByType<Canvas>().transform);

        HealthBarManager healthBar = healthBarObject.GetComponent<HealthBarManager>();
        if (healthBar == null)
        {
            Debug.LogError("Health bar prefab is missing HealthBarManager component.");
            Destroy(healthBarObject);
            return;
        }

        healthBar.Setup(character, targetTransform);
    }

    public void GetCharacterData()
    {
        data.Clear();

        PlayerDataManager playerDataManager = PlayerDataManager.Instance;
        SpawnManager spawnManager = SpawnManager.Instance;

        if (playerDataManager == null || playerDataManager.playerCharacterData == null)
        {
            Debug.LogError("PlayerDataManager or playerCharacterData is missing.");
            return;
        }

        data.Add(playerDataManager.playerCharacterData);

        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager is missing.");
            return;
        }

        if (spawnManager.HasPendingCustomCombatEnemyPool())
        {
            List<CharacterData> customEnemies = spawnManager.ConsumePendingCustomCombatEnemyPool();

            foreach (CharacterData enemy in customEnemies)
            {
                if (enemy != null)
                {
                    data.Add(enemy);
                }
            }

            if (customEnemies.Count == 0)
            {
                Debug.LogWarning("Custom combat enemy pool existed but contained no valid enemies.");
            }

            return;
        }

        AreaData encounterArea = spawnManager.GetAreaDataById(spawnManager.currentEncounterAreaId);

        // Fixed/fallback encounter data must come from the overworld area that started combat.
        // Do not fall back to GetCurrentAreaData() here, because while CombatPreparation runs
        // the current scene is the Combat scene, so that would read the Combat scene's area data.
        if (encounterArea == null)
        {
            Debug.LogError($"Could not find encounter area data for combat. currentEncounterAreaId='{spawnManager.currentEncounterAreaId}', currentEncounterId='{spawnManager.currentEncounterId}'.");
            return;
        }

        CharacterData enemyData = null;
        string encounterId = spawnManager.currentEncounterId;

        if (!string.IsNullOrEmpty(encounterId) && encounterId.StartsWith("random_"))
        {
            enemyData = encounterArea.FindRandomEnemyByEncounterId(encounterId);
        }
        else if (!string.IsNullOrEmpty(encounterId))
        {
            enemyData = encounterArea.FindFixedEnemyByEncounterId(encounterId);
        }

        if (enemyData == null && encounterArea.enableRandomEncounters)
        {
            enemyData = encounterArea.PickRandomEnemy();
        }

        if (enemyData != null)
        {
            data.Add(enemyData);
        }
        else
        {
            Debug.LogWarning($"No enemy found for encounter '{encounterId}' in area '{encounterArea.areaId}'. Combat will start with no enemies.");
        }
    }
}
