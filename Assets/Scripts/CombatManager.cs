using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public CharacterData data;

    public float threatLevel;
    public string threatType;
    public string state;
    public Character target;
    public bool isPlayer;
    public int gridLocation;

    public int team;

    public float health;

    public bool canDodge = true;
    public bool canParry = true;
    public bool canBlock = true;

    public string lastDefenseUsed = "";

    private Vector3 originalPosition;
    private Transform myTransform;

    void Awake()
    {
        myTransform = transform;
        originalPosition = myTransform.position;
    }

    public void BeginTurn(List<Character> opponentTeam)
    {
        if (state == "Staggered") return;
        if (state == "Dead") return;

        FindTargetByThreat(opponentTeam);
    }

    public void ResetHealth()
    {
        health = data.maxHealth;
    }

    private void FindTargetByThreat(List<Character> opponentTeam)
    {
        List<Character> validTargets = new List<Character>();
        foreach (Character c in opponentTeam)
        {
            if (c.team != this.team && c.state != "Dead")
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
            validTargets.Sort((a, b) => b.data.damage.CompareTo(a.data.damage));
        }
        else if (threatType == "HighestDEF")
        {
            validTargets.Sort((a, b) => b.data.defense.CompareTo(a.data.defense));
        }
        else if (threatType == "HighestSpeed")
        {
            validTargets.Sort((a, b) => b.data.speed.CompareTo(a.data.speed));
        }
        target = validTargets[0];
    }

    public bool AttemptDefense(float attackStartTime, float attackDuration, out string defenseType)
    {
        defenseType = "";
        bool defenseSucceeded = false;

        if (target == null) return false;

        if (canDodge && !defenseSucceeded)
        {
            defenseSucceeded = RollForDodge(attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Dodge";
        }

        if (canParry && !defenseSucceeded)
        {
            defenseSucceeded = RollForParry(attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Parry";
        }

        if (canBlock && !defenseSucceeded)
        {
            defenseSucceeded = RollForBlock(attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Block";
        }

        lastDefenseUsed = defenseType;
        return defenseSucceeded;
    }

    public bool RollForParry(float attackStartTime, float attackDuration)
    {
        if (target == null || target.data == null) return false;

        float parryChance = target.data.parryChance;
        float speedDiff = data.speed - target.data.speed;
        float accuracyDiff = data.accuracy - target.data.accuracy;

        float speedModifier = Mathf.Log10(1f + Mathf.Abs(speedDiff)) * Mathf.Sign(speedDiff);
        float accuracyModifier = Mathf.Log10(1f + Mathf.Abs(accuracyDiff)) * Mathf.Sign(accuracyDiff);

        parryChance *= 1 + speedModifier * 0.05f + accuracyModifier * 0.1f;
        parryChance = Mathf.Clamp(parryChance, 0.05f, 0.95f);

        bool success = false;

        if (isPlayer)
        {
            float baseWindowSize = 0.15f;
            float windowSize = baseWindowSize * (1f + parryChance * 0.5f);

            float windowStart = attackDuration * (1f - windowSize);
            float windowEnd = attackDuration;

            float currentTime = Time.time;
            float timeSinceAttackStart = currentTime - attackStartTime;

            if (Input.GetKeyDown(KeyCode.Space) &&
                timeSinceAttackStart >= windowStart &&
                timeSinceAttackStart <= windowEnd)
            {
                success = true;
            }
        }
        else
        {
            success = Random.value < parryChance;
        }

        return success;
    }

    public bool RollForDodge(float attackStartTime, float attackDuration)
    {
        if (target == null || target.data == null) return false;

        float dodgeChance = data.dodgeChance;
        float speedDiff = data.speed - target.data.speed;
        float accuracyDiff = data.accuracy - target.data.accuracy;

        float speedModifier = Mathf.Log10(1f + Mathf.Abs(speedDiff)) * Mathf.Sign(speedDiff);
        float accuracyModifier = Mathf.Log10(1f + Mathf.Abs(accuracyDiff)) * Mathf.Sign(accuracyDiff);

        dodgeChance *= 1 + speedModifier * 0.08f + accuracyModifier * 0.12f;
        dodgeChance = Mathf.Clamp(dodgeChance, 0.05f, 0.95f);

        bool success = false;

        if (isPlayer)
        {
            float baseWindowSize = 0.25f;
            float windowSize = baseWindowSize * (1f + dodgeChance * 0.4f);

            float windowStart = 0f;
            float windowEnd = windowStart + windowSize;

            float currentTime = Time.time;
            float timeSinceAttackStart = currentTime - attackStartTime;

            if (Input.GetKeyDown(KeyCode.LeftShift) &&
                timeSinceAttackStart >= windowStart &&
                timeSinceAttackStart <= windowEnd)
            {
                success = true;
            }
        }
        else
        {
            success = Random.value < dodgeChance;
        }

        return success;
    }

    public bool RollForBlock(float attackStartTime, float attackDuration)
    {
        if (target == null || target.data == null) return false;

        float blockChance = data.blockChance;
        float defenseDiff = data.defense - target.data.damage;

        float defenseModifier = Mathf.Log10(1f + Mathf.Abs(defenseDiff)) * Mathf.Sign(defenseDiff);

        blockChance *= 1 + defenseModifier * 0.15f;
        blockChance = Mathf.Clamp(blockChance, 0.1f, 0.9f);

        bool success = false;

        if (isPlayer)
        {
            float baseWindowSize = 0.35f;
            float windowSize = baseWindowSize * (1f + blockChance * 0.3f);

            float windowStart = attackDuration * 0.3f;
            float windowEnd = windowStart + windowSize;

            float currentTime = Time.time;
            float timeSinceAttackStart = currentTime - attackStartTime;

            if (Input.GetMouseButton(1) &&
                timeSinceAttackStart >= windowStart &&
                timeSinceAttackStart <= windowEnd)
            {
                success = true;
            }
        }
        else
        {
            success = Random.value < blockChance;
        }

        return success;
    }

    public IEnumerator AttackAnimation(Transform targetTransform, float duration)
    {
        if (targetTransform == null) yield break;

        Vector3 startPos = originalPosition;
        Vector3 targetPos = targetTransform.position;
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        Vector3 forwardPos = startPos + direction * (distance * 0.7f);

        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        while (elapsed < halfDuration)
        {
            myTransform.position = Vector3.Lerp(startPos, forwardPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            myTransform.position = Vector3.Lerp(forwardPos, startPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        myTransform.position = startPos;
    }
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    public List<Character> combatants;
    public int currentTurnIndex = 0;

    private float timeSinceCombatEnded; 

    [Header("Animation Settings")]
    public float attackMoveDistance = 1.5f;
    public float attackAnimationDuration = 0.5f;

    [Header("Buffer Time Settings")]
    public float bufferTimePreCombat = 3f;
    public float bufferTimePostCombat = 3f;

    void Awake()
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
        StartCoroutine(StartCombatWithBuffer(playerTeam, enemyTeam));
    }

    private IEnumerator StartCombatWithBuffer(List<Character> playerTeam, List<Character> enemyTeam)
    {
        Debug.Log($"Combat starting in {bufferTimePreCombat} seconds...");
        
        // ✅ Pre-combat buffer: Wait before starting combat
        yield return new WaitForSeconds(bufferTimePreCombat);

        // Now initialize combat
        combatants = new List<Character>();

        foreach (Character c in playerTeam)
        {
            c.team = 0;
            combatants.Add(c);
        }

        foreach (Character c in enemyTeam)
        {
            c.team = 1;
            combatants.Add(c);
        }

        combatants.Sort((a, b) => b.data.speed.CompareTo(a.data.speed));

        foreach (Character c in combatants)
        {
            c.threatLevel = 0;
            c.ResetHealth();
        }

        StartCoroutine(CombatLoop());
    }

    public IEnumerator CombatLoop()
    {
        while (combatants.Count > 0)
        {
            if (currentTurnIndex >= combatants.Count)
            {
                currentTurnIndex = 0;
            }

            Character current = combatants[currentTurnIndex];

            if (current.state == "Dead")
            {
                RemoveDeadCharacter(current);
                NextTurn();
                continue;
            }

            current.BeginTurn(GetOpponentTeam(current));

            if (current.target != null)
            {
                float attackStartTime = Time.time;
                float attackDuration = current.data.attackDuration;

                StartCoroutine(current.AttackAnimation(current.target.transform, attackAnimationDuration));

                bool defenseSucceeded = false;
                string defenseType = "";

                yield return StartCoroutine(HandleDefenseWindow(current, current.target, attackStartTime, attackDuration,
                    (success, type) => {
                        defenseSucceeded = success;
                        defenseType = type;
                    }));

                if (!defenseSucceeded)
                {
                    CalculateDamage(current, current.target, "None");
                }
                else
                {
                    ApplyDefenseResults(current, current.target, defenseType);
                }
            }
            else
            {
                Debug.Log($"{current.data.name} has no valid target. Skipping turn.");
            }

            RemoveAllDeadCharacters();

            // Check if combat is over
            bool playerTeamAlive = false;
            bool enemyTeamAlive = false;

            foreach (Character c in combatants)
            {
                if (c.team == 0 && c.state != "Dead") playerTeamAlive = true;
                if (c.team == 1 && c.state != "Dead") enemyTeamAlive = true;
            }

            if (!enemyTeamAlive)
            {
                SpawnManager spawnManager = SpawnManager.Instance;
                if (spawnManager != null)
                {
                    // Mark the entire encounter as defeated
                    PlayerController playerController = PlayerController.Instance;
                    if (!playerController.currentArea.enableRandomEncounters)
                    {
                        spawnManager.MarkEncounterDefeated(spawnManager.currentEncounterAreaId, spawnManager.currentEncounterId);
                    }
                    
                    Debug.Log($"Victory! Waiting {bufferTimePostCombat} seconds before returning...");
                    
                    // ✅ Post-combat buffer: Wait before scene transition
                    yield return new WaitForSeconds(bufferTimePostCombat);
                    
                    spawnManager.NextScene("CombatOver");
                }
                yield break;
            }
            else if (!playerTeamAlive)
            {
                Debug.Log("Player team defeated!");
                yield break;
            }

            NextTurn();
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator HandleDefenseWindow(Character attacker, Character defender, float attackStartTime, float attackDuration,
        System.Action<bool, string> onComplete)
    {
        bool defenseSucceeded = false;
        string defenseType = "";
        float elapsed = 0f;

        if (defender.isPlayer)
        {
            while (elapsed < attackDuration && !defenseSucceeded)
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    defenseSucceeded = defender.RollForDodge(attackStartTime, attackDuration);
                    if (defenseSucceeded) defenseType = "Dodge";
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    defenseSucceeded = defender.RollForParry(attackStartTime, attackDuration);
                    if (defenseSucceeded) defenseType = "Parry";
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    defenseSucceeded = defender.RollForBlock(attackStartTime, attackDuration);
                    if (defenseSucceeded) defenseType = "Block";
                }

                elapsed = Time.time - attackStartTime;
                yield return null;
            }
        }
        else
        {
            defenseSucceeded = defender.AttemptDefense(attackStartTime, attackDuration, out defenseType);
            yield return null;
        }

        onComplete?.Invoke(defenseSucceeded, defenseType);
    }

    private void ApplyDefenseResults(Character attacker, Character defender, string defenseType)
    {
        switch (defenseType)
        {
            case "Dodge":
                Debug.Log($"{defender.data.name} dodged the attack!");
                break;

            case "Parry":
                Debug.Log($"{defender.data.name} parried the attack!");
                CalculateDamage(defender, attacker, "Counter");
                break;

            case "Block":
                Debug.Log($"{defender.data.name} blocked the attack!");
                CalculateDamage(attacker, defender, "Blocked");
                break;
        }
    }

    private void CalculateDamage(Character attacker, Character defender, string defenseContext = "None")
    {
        if (defender == null || defender.data == null) return;

        float damage = attacker.data.damage - defender.data.defense;
        damage = Mathf.Max(damage, 1);

        switch (defenseContext)
        {
            case "Blocked":
                damage *= 0.5f;
                break;
            case "Counter":
                damage = 0;
                break;
            case "Dodge":
                damage = 0;
                break;
        }

        if (damage > 0)
        {
            defender.health -= damage;
            Debug.Log($"{attacker.data.name} dealt {damage} damage to {defender.data.name}");

            if (defender.health <= 0)
            {
                defender.state = "Dead";
                Debug.Log($"{defender.data.name} died!");
            }
        }
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
        List<Character> opponents = new List<Character>();
        foreach (Character c in combatants)
        {
            if (c.team != character.team && c.state != "Dead")
            {
                opponents.Add(c);
            }
        }
        return opponents;
    }

    private void RemoveDeadCharacter(Character deadCharacter)
    {
        if (deadCharacter == null) return;

        combatants.Remove(deadCharacter);

        if (deadCharacter.gameObject != null)
        {
            Destroy(deadCharacter.gameObject);
        }

        Debug.Log($"{deadCharacter.data.name} removed from combat.");
    }

    private void RemoveAllDeadCharacters()
    {
        for (int i = combatants.Count - 1; i >= 0; i--)
        {
            if (combatants[i].state == "Dead")
            {
                RemoveDeadCharacter(combatants[i]);
            }
        }
    }
}