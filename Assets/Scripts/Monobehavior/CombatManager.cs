using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public CharacterData data;

    public float threatLevel;
    public string threatType;
    public string state = "Alive";
    public Character target;
    public bool isPlayer;
    public int gridLocation;
    public int team;

    public float health;

    public bool canDodge = true;
    public bool canParry = true;
    public bool canBlock = true;

    public string lastDefenseUsed = "";

    private Transform myTransform;

    private void Awake()
    {
        myTransform = transform;
    }

    public void BeginTurn(List<Character> opponentTeam)
    {
        if (state == "Staggered") return;
        if (state == "Dead") return;

        FindTargetByThreat(opponentTeam);
    }

    public void ResetHealth()
    {
        if (isPlayer)
        {
            PlayerData playerData = GetPlayerData();

            if (playerData != null)
            {
                health = Mathf.Max(0f, playerData.currentHealth);
                state = health > 0f ? "Alive" : "Dead";
                return;
            }
        }

        if (data == null)
        {
            return;
        }

        health = data.maxHealth;

        if (state == "Dead")
        {
            state = "Alive";
        }

        if (string.IsNullOrEmpty(state))
        {
            state = "Alive";
        }
    }

    private void FindTargetByThreat(List<Character> opponentTeam)
    {
        List<Character> validTargets = new();

        foreach (Character c in opponentTeam)
        {
            if (c != null && c.team != team && c.state != "Dead")
            {
                validTargets.Add(c);
            }
        }

        if (validTargets.Count == 0)
        {
            target = null;
            return;
        }

        if (threatType == "HighestDPS")
        {
            validTargets.Sort((a, b) => b.GetDamage().CompareTo(a.GetDamage()));
        }
        else if (threatType == "HighestDEF")
        {
            validTargets.Sort((a, b) => b.GetBaseDefense().CompareTo(a.GetBaseDefense()));
        }
        else if (threatType == "HighestSpeed")
        {
            validTargets.Sort((a, b) => b.GetSpeed().CompareTo(a.GetSpeed()));
        }

        target = validTargets[0];
    }

    public bool AttemptDefense(Character attacker, float attackStartTime, float attackDuration, out string defenseType)
    {
        defenseType = "";
        bool defenseSucceeded = false;

        if (attacker == null || !HasCombatStats() || !attacker.HasCombatStats())
        {
            return false;
        }

        if (canDodge && !defenseSucceeded)
        {
            defenseSucceeded = RollForDodge(attacker, attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Dodge";
        }

        if (canParry && !defenseSucceeded)
        {
            defenseSucceeded = RollForParry(attacker, attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Parry";
        }

        if (canBlock && !defenseSucceeded)
        {
            defenseSucceeded = RollForBlock(attacker, attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Block";
        }

        lastDefenseUsed = defenseType;
        return defenseSucceeded;
    }

    public bool RollForParry(Character attacker, float attackStartTime, float attackDuration)
    {
        if (attacker == null || !HasCombatStats() || !attacker.HasCombatStats())
        {
            return false;
        }

        float parryChance = GetParryChance();
        float speedDiff = GetSpeed() - attacker.GetSpeed();
        float accuracyDiff = GetAccuracy() - attacker.GetAccuracy();

        float speedModifier = Mathf.Log10(1f + Mathf.Abs(speedDiff)) * Mathf.Sign(speedDiff);
        float accuracyModifier = Mathf.Log10(1f + Mathf.Abs(accuracyDiff)) * Mathf.Sign(accuracyDiff);

        parryChance *= 1f + speedModifier * 0.05f + accuracyModifier * 0.1f;
        parryChance = Mathf.Clamp(parryChance, 0.05f, 0.95f);

        if (isPlayer)
        {
            float baseWindowSize = 0.15f;
            float windowSize = baseWindowSize * (1f + parryChance * 0.5f);

            float windowStart = attackDuration * (1f - windowSize);
            float windowEnd = attackDuration;

            float timeSinceAttackStart = Time.time - attackStartTime;

            return Input.GetKeyDown(KeyCode.Space)
                && timeSinceAttackStart >= windowStart
                && timeSinceAttackStart <= windowEnd;
        }

        return Random.value < parryChance;
    }

    public bool RollForDodge(Character attacker, float attackStartTime, float attackDuration)
    {
        if (attacker == null || !HasCombatStats() || !attacker.HasCombatStats())
        {
            return false;
        }

        float dodgeChance = GetDodgeChance();
        float speedDiff = GetSpeed() - attacker.GetSpeed();
        float accuracyDiff = GetAccuracy() - attacker.GetAccuracy();

        float speedModifier = Mathf.Log10(1f + Mathf.Abs(speedDiff)) * Mathf.Sign(speedDiff);
        float accuracyModifier = Mathf.Log10(1f + Mathf.Abs(accuracyDiff)) * Mathf.Sign(accuracyDiff);

        dodgeChance *= 1f + speedModifier * 0.08f + accuracyModifier * 0.12f;
        dodgeChance = Mathf.Clamp(dodgeChance, 0.05f, 0.95f);

        if (isPlayer)
        {
            float baseWindowSize = 0.25f;
            float windowSize = baseWindowSize * (1f + dodgeChance * 0.4f);

            float windowStart = 0f;
            float windowEnd = windowStart + windowSize;

            float timeSinceAttackStart = Time.time - attackStartTime;

            return Input.GetKeyDown(KeyCode.LeftShift)
                && timeSinceAttackStart >= windowStart
                && timeSinceAttackStart <= windowEnd;
        }

        return Random.value < dodgeChance;
    }

    public bool RollForBlock(Character attacker, float attackStartTime, float attackDuration)
    {
        if (attacker == null || !HasCombatStats() || !attacker.HasCombatStats())
        {
            return false;
        }

        float blockChance = GetBlockChance();
        float defenseDiff = GetBaseDefense() - attacker.GetDamage();

        float defenseModifier = Mathf.Log10(1f + Mathf.Abs(defenseDiff)) * Mathf.Sign(defenseDiff);

        blockChance *= 1f + defenseModifier * 0.15f;
        blockChance = Mathf.Clamp(blockChance, 0.1f, 0.9f);

        if (isPlayer)
        {
            float baseWindowSize = 0.35f;
            float windowSize = baseWindowSize * (1f + blockChance * 0.3f);

            float windowStart = attackDuration * 0.3f;
            float windowEnd = windowStart + windowSize;

            float timeSinceAttackStart = Time.time - attackStartTime;

            return Input.GetMouseButtonDown(1)
                && timeSinceAttackStart >= windowStart
                && timeSinceAttackStart <= windowEnd;
        }

        return Random.value < blockChance;
    }

    public IEnumerator AttackAnimation(Transform targetTransform, float duration)
    {
        if (myTransform == null || targetTransform == null)
        {
            yield break;
        }

        Vector3 startPos = myTransform.position;
        Vector3 targetPos = targetTransform.position;
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        if (direction == Vector3.zero)
        {
            yield break;
        }

        Vector3 forwardPos = startPos + direction * (distance * 0.7f);

        float elapsed = 0f;
        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);

        while (elapsed < halfDuration)
        {
            if (myTransform == null)
            {
                yield break;
            }

            myTransform.position = Vector3.Lerp(startPos, forwardPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (myTransform == null)
        {
            yield break;
        }

        myTransform.position = forwardPos;

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            if (myTransform == null)
            {
                yield break;
            }

            myTransform.position = Vector3.Lerp(forwardPos, startPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (myTransform != null)
        {
            myTransform.position = startPos;
        }
    }

    public bool HasCombatStats()
    {
        if (isPlayer && GetPlayerData() != null)
        {
            return true;
        }

        return data != null;
    }

    public string GetDisplayName()
    {
        if (data != null)
        {
            return data.name;
        }

        return gameObject.name;
    }

    public float GetAttackDuration(float fallback)
    {
        if (data != null && data.attackDuration > 0f)
        {
            return data.attackDuration;
        }

        return fallback;
    }

    public float GetDamage()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentDamage;
        }

        return data != null ? data.damage : 0f;
    }

    public float GetSpeed()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentSpeed;
        }

        return data != null ? data.speed : 0f;
    }

    public float GetBaseDefense()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentBaseDefense;
        }

        return data != null ? data.defense : 0f;
    }

    public float GetTrueDefense()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentTrueDefense;
        }

        return data != null ? data.trueDefense : 0f;
    }

    public float GetAccuracy()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentAccuracy;
        }

        return data != null ? data.accuracy : 0f;
    }

    public float GetParryChance()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentParryChance;
        }

        return data != null ? data.parryChance : 0f;
    }

    public float GetBlockChance()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentBlockChance;
        }

        return data != null ? data.blockChance : 0f;
    }

    public float GetDodgeChance()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentDodgeChance;
        }

        return data != null ? data.dodgeChance : 0f;
    }

    public float GetBlockBypassRate()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentBlockBypassRate;
        }

        return data != null ? data.blockBypassRate : 0f;
    }

    public float GetDodgeBypassRate()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentDodgeBypassRate;
        }

        return data != null ? data.dodgeBypassRate : 0f;
    }

    public float GetEnergyReserves()
    {
        PlayerData playerData = GetPlayerData();

        if (isPlayer && playerData != null)
        {
            return playerData.currentEnergyReserves;
        }

        return data != null ? data.energyReserves : 0f;
    }

    public void SetCombatHealth(float value)
    {
        health = Mathf.Max(0f, value);

        if (isPlayer)
        {
            PlayerData playerData = GetPlayerData();

            if (playerData != null)
            {
                playerData.currentHealth = health;
            }
        }
    }

    private PlayerData GetPlayerData()
    {
        if (!isPlayer)
        {
            return null;
        }

        if (PlayerDataManager.Instance == null)
        {
            return null;
        }

        return PlayerDataManager.Instance.playerData;
    }
}

[System.Serializable]
public class PendingLootEntry
{
    public ItemData item;
    public int quantity;

    public bool dropOnceOnly;
    public string onceOnlyDropId;

    public PendingLootEntry(ItemData item, int quantity, bool dropOnceOnly, string onceOnlyDropId)
    {
        this.item = item;
        this.quantity = quantity;
        this.dropOnceOnly = dropOnceOnly;
        this.onceOnlyDropId = onceOnlyDropId;
    }
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public List<Character> combatants = new();
    public int currentTurnIndex = 0;

    [Header("Animation Settings")]
    public float attackMoveDistance = 1.5f;
    public float attackAnimationDuration = 0.5f;

    [Header("Buffer Time Settings")]
    public float bufferTimePreCombat = 3f;
    public float bufferTimePostCombat = 3f;

    [Header("Loot")]
    [SerializeField] private bool enableLootDrops = false;
    [SerializeField] private bool giveLootOnlyOnVictory = true;

    [Header("Defense Result Settings")]
    [SerializeField] private bool parryCounterAttacks = false;
    [SerializeField] private bool blockTakesReducedDamage = true;
    [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;

    private readonly List<PendingLootEntry> pendingLoot = new();
    private readonly List<Character> activePlayerTeam = new();

    private bool combatEnding = false;
    private bool combatActive = false;

    public bool IsCombatActive => combatActive || combatEnding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartCombat(List<Character> playerTeam, List<Character> enemyTeam)
    {
        StopAllCoroutines();

        PlayerController.Instance?.DisableMovementForCombat();

        combatActive = true;
        combatEnding = false;
        pendingLoot.Clear();
        activePlayerTeam.Clear();

        StartCoroutine(StartCombatWithBuffer(playerTeam, enemyTeam));
    }

    private IEnumerator StartCombatWithBuffer(List<Character> playerTeam, List<Character> enemyTeam)
    {
        Debug.Log($"Combat starting in {bufferTimePreCombat} seconds...");

        yield return new WaitForSeconds(bufferTimePreCombat);

        combatants = new List<Character>();
        currentTurnIndex = 0;

        foreach (Character c in playerTeam)
        {
            if (c == null || !c.HasCombatStats())
            {
                continue;
            }

            c.team = 0;
            c.state = "Alive";
            c.ResetHealth();

            if (!activePlayerTeam.Contains(c))
            {
                activePlayerTeam.Add(c);
            }

            if (c.state != "Dead")
            {
                combatants.Add(c);
            }
        }

        foreach (Character c in enemyTeam)
        {
            if (c == null || c.data == null)
            {
                continue;
            }

            c.team = 1;
            c.state = "Alive";
            c.ResetHealth();
            combatants.Add(c);
        }

        combatants.Sort((a, b) => b.GetSpeed().CompareTo(a.GetSpeed()));

        foreach (Character c in combatants)
        {
            c.threatLevel = 0;
        }

        if (TryGetCombatEndResult(out bool playerWonBeforeFirstTurn))
        {
            yield return StartCoroutine(HandleCombatEnd(playerWonBeforeFirstTurn));
            yield break;
        }

        yield return StartCoroutine(CombatLoop());
    }

    public IEnumerator CombatLoop()
    {
        while (!combatEnding && combatants.Count > 0)
        {
            if (TryGetCombatEndResult(out bool playerWonBeforeTurn))
            {
                yield return StartCoroutine(HandleCombatEnd(playerWonBeforeTurn));
                yield break;
            }

            if (currentTurnIndex >= combatants.Count)
            {
                currentTurnIndex = 0;
            }

            Character current = combatants[currentTurnIndex];

            if (!IsValidActiveCombatant(current))
            {
                RemoveDeadCharacter(current);
                continue;
            }

            current.BeginTurn(GetOpponentTeam(current));
            Character defender = current.target;

            if (IsValidActiveCombatant(defender))
            {
                yield return StartCoroutine(HandleAttackSequence(current, defender));
            }
            else
            {
                Debug.Log($"{current.GetDisplayName()} has no valid target. Skipping turn.");
            }

            RemoveAllDeadCharacters();

            if (TryGetCombatEndResult(out bool playerWonAfterTurn))
            {
                yield return StartCoroutine(HandleCombatEnd(playerWonAfterTurn));
                yield break;
            }

            NextTurn();

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator HandleAttackSequence(Character attacker, Character defender)
    {
        if (!IsValidActiveCombatant(attacker) || !IsValidActiveCombatant(defender))
        {
            yield break;
        }

        float attackDuration = attacker.GetAttackDuration(attackAnimationDuration);
        float attackStartTime = Time.time;

        bool defenseSucceeded = false;
        string defenseType = "";

        Coroutine attackAnimationCoroutine = null;

        if (defender != null)
        {
            attackAnimationCoroutine = StartCoroutine(attacker.AttackAnimation(defender.transform, attackDuration));
        }

        yield return StartCoroutine(HandleDefenseWindow(
            attacker,
            defender,
            attackStartTime,
            attackDuration,
            (success, type) =>
            {
                defenseSucceeded = success;
                defenseType = type;
            }));

        if (attackAnimationCoroutine != null)
        {
            yield return attackAnimationCoroutine;
        }

        if (!IsValidActiveCombatant(attacker) || !IsValidActiveCombatant(defender))
        {
            yield break;
        }

        if (!defenseSucceeded)
        {
            CalculateDamage(attacker, defender, "None");
        }
        else
        {
            ApplyDefenseResults(attacker, defender, defenseType);
        }
    }

    private IEnumerator HandleDefenseWindow(
        Character attacker,
        Character defender,
        float attackStartTime,
        float attackDuration,
        System.Action<bool, string> onComplete)
    {
        bool defenseSucceeded = false;
        string defenseType = "";

        if (!IsValidActiveCombatant(attacker) || !IsValidActiveCombatant(defender))
        {
            onComplete?.Invoke(false, "");
            yield break;
        }

        if (defender.isPlayer)
        {
            while (!combatEnding && Time.time - attackStartTime < attackDuration)
            {
                if (!IsValidActiveCombatant(attacker) || !IsValidActiveCombatant(defender))
                {
                    break;
                }

                if (!defenseSucceeded)
                {
                    if (Input.GetKeyDown(KeyCode.LeftShift))
                    {
                        defenseSucceeded = defender.RollForDodge(attacker, attackStartTime, attackDuration);
                        if (defenseSucceeded) defenseType = "Dodge";
                    }
                    else if (Input.GetKeyDown(KeyCode.Space))
                    {
                        defenseSucceeded = defender.RollForParry(attacker, attackStartTime, attackDuration);
                        if (defenseSucceeded) defenseType = "Parry";
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        defenseSucceeded = defender.RollForBlock(attacker, attackStartTime, attackDuration);
                        if (defenseSucceeded) defenseType = "Block";
                    }
                }

                yield return null;
            }
        }
        else
        {
            defenseSucceeded = defender.AttemptDefense(attacker, attackStartTime, attackDuration, out defenseType);

            while (!combatEnding && Time.time - attackStartTime < attackDuration)
            {
                if (!IsValidActiveCombatant(attacker) || !IsValidActiveCombatant(defender))
                {
                    break;
                }

                yield return null;
            }
        }

        if (defender != null)
        {
            defender.lastDefenseUsed = defenseType;
        }

        onComplete?.Invoke(defenseSucceeded, defenseType);
    }

    private void ApplyDefenseResults(Character attacker, Character defender, string defenseType)
    {
        if (attacker == null || defender == null)
        {
            return;
        }

        switch (defenseType)
        {
            case "Dodge":
                Debug.Log($"{defender.GetDisplayName()} dodged the attack. No damage taken.");
                break;

            case "Parry":
                Debug.Log($"{defender.GetDisplayName()} parried the attack. No damage taken.");

                if (parryCounterAttacks)
                {
                    CalculateDamage(defender, attacker, "Counter");
                }
                break;

            case "Block":
                Debug.Log($"{defender.GetDisplayName()} blocked the attack.");

                if (blockTakesReducedDamage)
                {
                    CalculateDamage(attacker, defender, "Blocked");
                }
                break;

            default:
                CalculateDamage(attacker, defender, "None");
                break;
        }
    }

    private void CalculateDamage(Character attacker, Character defender, string defenseContext = "None")
    {
        if (attacker == null || defender == null)
        {
            return;
        }

        if (!attacker.HasCombatStats() || !defender.HasCombatStats())
        {
            return;
        }

        if (attacker.state == "Dead" || defender.state == "Dead")
        {
            return;
        }

        float damage = attacker.GetDamage() - defender.GetBaseDefense();
        damage = Mathf.Max(damage, 1f);

        switch (defenseContext)
        {
            case "Blocked":
                damage *= blockDamageMultiplier;
                break;

            case "Counter":
                damage = Mathf.Max(attacker.GetDamage() - defender.GetBaseDefense(), 1f);
                break;

            case "Dodge":
                damage = 0f;
                break;
        }

        if (damage <= 0f)
        {
            return;
        }

        defender.SetCombatHealth(defender.health - damage);

        Debug.Log($"{attacker.GetDisplayName()} dealt {damage} damage to {defender.GetDisplayName()}");

        if (defender.health <= 0f)
        {
            defender.state = "Dead";
            Debug.Log($"{defender.GetDisplayName()} died!");
        }

        SavePlayerIfPossible();
    }

    private bool IsValidActiveCombatant(Character character)
    {
        return character != null && character.state != "Dead" && character.HasCombatStats();
    }

    private bool TryGetCombatEndResult(out bool playerWon)
    {
        bool playerTeamAlive = false;
        bool enemyTeamAlive = false;

        foreach (Character c in combatants)
        {
            if (!IsValidActiveCombatant(c))
            {
                continue;
            }

            if (c.team == 0)
            {
                playerTeamAlive = true;
            }
            else if (c.team == 1)
            {
                enemyTeamAlive = true;
            }
        }

        if (!playerTeamAlive && !enemyTeamAlive)
        {
            playerWon = false;
            return true;
        }

        if (!enemyTeamAlive)
        {
            playerWon = true;
            return true;
        }

        if (!playerTeamAlive)
        {
            playerWon = false;
            return true;
        }

        playerWon = false;
        return false;
    }

    private void RestorePlayerTeamAfterDefeat()
    {
        foreach (Character playerCharacter in activePlayerTeam)
        {
            if (playerCharacter == null || !playerCharacter.isPlayer)
            {
                continue;
            }

            playerCharacter.state = "Alive";
            playerCharacter.target = null;
            playerCharacter.SetCombatHealth(1f);
        }

        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.playerData != null)
        {
            PlayerDataManager.Instance.playerData.currentHealth = 1f;
        }
    }


    private void RestorePlayerTeamAfterCombat(bool playerWon)
    {
        foreach (Character playerCharacter in activePlayerTeam)
        {
            if (playerCharacter == null || !playerCharacter.isPlayer)
            {
                continue;
            }

            playerCharacter.target = null;
            playerCharacter.lastDefenseUsed = "";

            if (playerWon && playerCharacter.state != "Dead")
            {
                playerCharacter.state = "Alive";
            }
        }
    }

    private void RestorePlayerControlAfterCombat()
    {
        // Directly unlock the persistent overworld player first. This is the
        // important part for combat launched by a film end command, because
        // the film system may have disabled PlayerController.canMove before
        // combat started.
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableMovementAfterCombat();
        }

        GameObject playerObject = null;

        if (PlayerController.Instance != null)
        {
            playerObject = PlayerController.Instance.gameObject;
        }

        if (playerObject == null)
        {
            foreach (Character playerCharacter in activePlayerTeam)
            {
                if (playerCharacter != null && playerCharacter.isPlayer)
                {
                    playerObject = playerCharacter.gameObject;
                    break;
                }
            }
        }

        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject == null)
        {
            return;
        }

        MonoBehaviour[] behaviours = playerObject.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            TrySetBoolMember(behaviour, "canMove", true);
            TrySetBoolMember(behaviour, "CanMove", true);
            TrySetBoolMember(behaviour, "movementEnabled", true);
            TrySetBoolMember(behaviour, "MovementEnabled", true);
            TrySetBoolMember(behaviour, "movementDisabledByCombat", false);
            TrySetBoolMember(behaviour, "isMovementDisabled", false);
            TrySetBoolMember(behaviour, "movementDisabled", false);
            TrySetBoolMember(behaviour, "disableMovement", false);
            TrySetBoolMember(behaviour, "isInCombat", false);
            TrySetBoolMember(behaviour, "InCombat", false);
        }
    }

    private void TrySetBoolMember(MonoBehaviour targetBehaviour, string memberName, bool value)
    {
        if (targetBehaviour == null || string.IsNullOrEmpty(memberName))
        {
            return;
        }

        System.Type type = targetBehaviour.GetType();

        System.Reflection.FieldInfo field = type.GetField(
            memberName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(targetBehaviour, value);
            return;
        }

        System.Reflection.PropertyInfo property = type.GetProperty(
            memberName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
        {
            property.SetValue(targetBehaviour, value);
        }
    }

    private IEnumerator HandleCombatEnd(bool playerWon)
    {
        if (combatEnding)
        {
            yield break;
        }

        combatEnding = true;

        if (playerWon)
        {
            Debug.Log("Enemy team defeated. Returning from combat.");

            if (enableLootDrops && giveLootOnlyOnVictory)
            {
                GivePendingLootToInventory();
            }
            else
            {
                pendingLoot.Clear();
                Debug.Log("Loot discarded because loot rewards are disabled for now.");
            }
        }
        else
        {
            pendingLoot.Clear();
            Debug.Log("Player team defeated. Returning from combat without loot.");
        }

        if (!playerWon)
        {
            RestorePlayerTeamAfterDefeat();
        }

        RestorePlayerTeamAfterCombat(playerWon);
        RestorePlayerControlAfterCombat();

        SavePlayerIfPossible();

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager == null)
        {
            Debug.LogWarning("Cannot return from combat because SpawnManager is missing.");
            yield break;
        }

        if (playerWon)
        {
            string encounterId = spawnManager.currentEncounterId;
            string encounterAreaId = spawnManager.currentEncounterAreaId;

            bool isRandomEncounter = !string.IsNullOrEmpty(encounterId) &&
                                     encounterId.StartsWith("random_");

            if (!isRandomEncounter &&
                !string.IsNullOrEmpty(encounterAreaId) &&
                !string.IsNullOrEmpty(encounterId))
            {
                spawnManager.MarkEncounterDefeated(encounterAreaId, encounterId);
            }
        }

        FilmSceneManager.Instance?.ResolveSceneEndCombatResult(playerWon);

        Debug.Log($"Combat ended. Waiting {bufferTimePostCombat} seconds before returning...");

        yield return new WaitForSeconds(bufferTimePostCombat);

        spawnManager.ReturnFromCombat(playerWon);

        // Keep IsCombatActive true until SpawnManager has fully returned to the
        // overworld. Otherwise queued cinematics can start during the same-frame
        // combat return window, before the correct SceneCharacterRegistry exists.
        yield return new WaitUntil(() =>
            SpawnManager.Instance == null ||
            (!SpawnManager.Instance.IsSceneTransitioning && !SpawnManager.Instance.IsCurrentSceneCombat)
        );

        yield return null;
        RestorePlayerControlAfterCombat();

        yield return new WaitForEndOfFrame();
        RestorePlayerControlAfterCombat();

        combatActive = false;

        combatants.Clear();
        currentTurnIndex = 0;
    }

    private void NextTurn()
    {
        currentTurnIndex++;

        if (currentTurnIndex >= combatants.Count)
        {
            currentTurnIndex = 0;
        }
    }

    private List<Character> GetOpponentTeam(Character character)
    {
        List<Character> opponents = new();

        foreach (Character c in combatants)
        {
            if (c != null && c.team != character.team && c.state != "Dead")
            {
                opponents.Add(c);
            }
        }

        return opponents;
    }

    private void RemoveDeadCharacter(Character deadCharacter)
    {
        if (deadCharacter == null)
        {
            combatants.RemoveAll(c => c == null);
            return;
        }

        string removedName = deadCharacter.GetDisplayName();
        bool wasPlayer = deadCharacter.isPlayer;

        RollDropsFrom(deadCharacter);

        int removedIndex = combatants.IndexOf(deadCharacter);
        combatants.Remove(deadCharacter);

        if (removedIndex >= 0 && removedIndex < currentTurnIndex)
        {
            currentTurnIndex--;
        }

        if (deadCharacter.team == 1 && deadCharacter.data != null)
        {
            string defeatedEnemyId = GameIdUtility.GetCharacterId(deadCharacter.data);

            if (string.IsNullOrWhiteSpace(defeatedEnemyId))
            {
                defeatedEnemyId = GameIdUtility.ToStableId(removedName);
            }

            QuestDataManager.Instance?.ReportEnemyDefeated(defeatedEnemyId);
        }

        // Never destroy the player object here. SpawnManager and camera return logic
        // can still need the player's Transform after a defeat.
        if (!wasPlayer && deadCharacter.gameObject != null)
        {
            Destroy(deadCharacter.gameObject);
        }

        Debug.Log($"{removedName} removed from combat.");
    }

    private void RemoveAllDeadCharacters()
    {
        for (int i = combatants.Count - 1; i >= 0; i--)
        {
            if (combatants[i] == null || combatants[i].state == "Dead")
            {
                RemoveDeadCharacter(combatants[i]);
            }
        }
    }

    private void RollDropsFrom(Character defeatedCharacter)
    {
        if (!enableLootDrops)
        {
            return;
        }

        if (defeatedCharacter == null)
        {
            return;
        }

        if (defeatedCharacter.data == null)
        {
            return;
        }

        if (defeatedCharacter.team != 1)
        {
            return;
        }

        if (defeatedCharacter.data.dropsPool == null)
        {
            return;
        }

        foreach (ItemDrop drop in defeatedCharacter.data.dropsPool)
        {
            if (drop == null)
            {
                continue;
            }

            if (drop.item == null)
            {
                continue;
            }

            string dropId = GetDropId(drop);

            if (drop.dropOnceOnly &&
                InventoryManager.Instance != null &&
                InventoryManager.Instance.HasCollectedOnceOnlyDrop(dropId))
            {
                Debug.Log($"Skipping once-only drop because it was already collected: {dropId}");
                continue;
            }

            float chance = Mathf.Clamp01(drop.dropChance);
            bool shouldDrop = drop.isPermanentDrop || Random.value <= chance;

            if (!shouldDrop)
            {
                Debug.Log($"Loot failed roll: {drop.item.itemName} | Chance: {chance * 100f}%");
                continue;
            }

            int minQuantity = Mathf.Max(1, drop.minQuantity);
            int maxQuantity = Mathf.Max(minQuantity, drop.maxQuantity);
            int quantity = Random.Range(minQuantity, maxQuantity + 1);

            AddPendingLoot(drop.item, quantity, drop.dropOnceOnly, dropId);

            Debug.Log($"Loot rolled: {drop.item.itemName} x{quantity} | Chance: {chance * 100f}%");
        }

        if (!giveLootOnlyOnVictory)
        {
            GivePendingLootToInventory();
        }
    }

    private string GetDropId(ItemDrop drop)
    {
        if (drop == null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(drop.uniqueDropId))
        {
            return drop.uniqueDropId.Trim();
        }

        if (drop.item != null && !string.IsNullOrWhiteSpace(drop.item.itemId))
        {
            return drop.item.itemId.Trim();
        }

        if (drop.item != null)
        {
            return drop.item.name;
        }

        return "";
    }

    private void AddPendingLoot(ItemData item, int quantity, bool dropOnceOnly, string onceOnlyDropId)
    {
        if (item == null || quantity <= 0)
        {
            return;
        }

        foreach (PendingLootEntry entry in pendingLoot)
        {
            if (entry.item == item &&
                entry.dropOnceOnly == dropOnceOnly &&
                entry.onceOnlyDropId == onceOnlyDropId)
            {
                entry.quantity += quantity;
                return;
            }
        }

        pendingLoot.Add(new PendingLootEntry(item, quantity, dropOnceOnly, onceOnlyDropId));
    }

    private void GivePendingLootToInventory()
    {
        if (pendingLoot.Count == 0)
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("Cannot give loot because InventoryManager is missing.");
            pendingLoot.Clear();
            return;
        }

        foreach (PendingLootEntry loot in pendingLoot)
        {
            if (loot == null || loot.item == null || loot.quantity <= 0)
            {
                continue;
            }

            bool added = InventoryManager.Instance.AddItem(loot.item, loot.quantity);
            QuestDataManager.Instance?.ReportItemCollected(loot.item, loot.quantity);

            if (added)
            {
                Debug.Log($"Added loot to inventory: {loot.item.itemName} x{loot.quantity}");

                if (loot.dropOnceOnly)
                {
                    InventoryManager.Instance.MarkOnceOnlyDropCollected(loot.onceOnlyDropId);
                    Debug.Log($"Marked once-only drop collected: {loot.onceOnlyDropId}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not fully add loot '{loot.item.itemName}' x{loot.quantity} because inventory is full.");
            }
        }

        pendingLoot.Clear();
    }

    private void SavePlayerIfPossible()
    {
        if (PlayerDataManager.Instance == null)
        {
            return;
        }

        PlayerDataManager.Instance.SavePlayer();
    }
}