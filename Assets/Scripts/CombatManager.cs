using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Assuming CharacterData is defined elsewhere
// [System.Serializable]
// public class CharacterData { ... }

public class Character : MonoBehaviour
{
    public CharacterData data;

    public float threatLevel;
    public string threatType;
    public string state;
    public Character target;
    public bool isPlayer; // True if controlled by player, false if AI (including companions)
    public int gridLocation;

    public int team; // 0 = Player team, 1 = Enemy team

    public float health;

    // Defense capabilities (configurable per character)
    public bool canDodge = true;
    public bool canParry = true;
    public bool canBlock = true;

    // Track last defense for feedback
    public string lastDefenseUsed = "";

    // Animation properties
    private Vector3 originalPosition;
    private Transform myTransform;

    void Awake()
    {
        myTransform = transform;
        originalPosition = myTransform.position;
    }

    /// <summary>
    /// Starts the character's turn. Skips if staggered or dead.
    /// </summary>
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

    /// <summary>
    /// Selects a target from the opponent team based on threat type.
    /// </summary>
    private void FindTargetByThreat(List<Character> opponentTeam)
    {
        // Filter to only opponents from the other team
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
            target = null; // No valid target
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

    /// <summary>
    /// Attempts all available defenses in priority order: Dodge → Parry → Block
    /// </summary>
    public bool AttemptDefense(float attackStartTime, float attackDuration, out string defenseType)
    {
        defenseType = "";
        bool defenseSucceeded = false;

        // If no target, cannot defend
        if (target == null) return false;

        // Priority 1: Dodge
        if (canDodge && !defenseSucceeded)
        {
            defenseSucceeded = RollForDodge(attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Dodge";
        }

        // Priority 2: Parry (if dodge failed)
        if (canParry && !defenseSucceeded)
        {
            defenseSucceeded = RollForParry(attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Parry";
        }

        // Priority 3: Block (last resort)
        if (canBlock && !defenseSucceeded)
        {
            defenseSucceeded = RollForBlock(attackStartTime, attackDuration);
            if (defenseSucceeded) defenseType = "Block";
        }

        lastDefenseUsed = defenseType;
        return defenseSucceeded;
    }

    /// <summary>
    /// Calculates and resolves a parry attempt. Window scales with parry chance.
    /// </summary>
    public bool RollForParry(float attackStartTime, float attackDuration)
    {
        // Guard against null target
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
            // Window scales with parry chance: higher chance = larger window
            float baseWindowSize = 0.15f; // Base 15% of attack duration
            float windowSize = baseWindowSize * (1f + parryChance * 0.5f); // Up to 75% larger with max chance

            // Position window near the end of attack
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

    /// <summary>
    /// Calculates and resolves a dodge attempt. Window scales with dodge chance.
    /// </summary>
    public bool RollForDodge(float attackStartTime, float attackDuration)
    {
        // Guard against null target
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
            // Window scales with dodge chance: higher chance = larger window
            float baseWindowSize = 0.25f; // Base 25% of attack duration
            float windowSize = baseWindowSize * (1f + dodgeChance * 0.4f); // Up to 60% larger with max chance

            // Position window early in attack
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

    /// <summary>
    /// Calculates and resolves a block attempt. Window scales with block chance.
    /// </summary>
    public bool RollForBlock(float attackStartTime, float attackDuration)
    {
        // Guard against null target
        if (target == null || target.data == null) return false;

        float blockChance = data.blockChance;
        float defenseDiff = data.defense - target.data.damage;

        float defenseModifier = Mathf.Log10(1f + Mathf.Abs(defenseDiff)) * Mathf.Sign(defenseDiff);

        blockChance *= 1 + defenseModifier * 0.15f;
        blockChance = Mathf.Clamp(blockChance, 0.1f, 0.9f);

        bool success = false;

        if (isPlayer)
        {
            // Window scales with block chance: higher chance = larger window
            float baseWindowSize = 0.35f; // Base 35% of attack duration
            float windowSize = baseWindowSize * (1f + blockChance * 0.3f); // Up to 65% larger with max chance

            // Position window in middle of attack
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

    /// <summary>
    /// Lerps the character towards the target and back for attack animation
    /// </summary>
    public IEnumerator AttackAnimation(Transform targetTransform, float duration)
    {
        if (targetTransform == null) yield break;

        Vector3 startPos = originalPosition;
        Vector3 targetPos = targetTransform.position;
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        // Move forward (70% of the way to target)
        Vector3 forwardPos = startPos + direction * (distance * 0.7f);

        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        // Move towards target
        while (elapsed < halfDuration)
        {
            myTransform.position = Vector3.Lerp(startPos, forwardPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Move back to original position
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            myTransform.position = Vector3.Lerp(forwardPos, startPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we're exactly at original position
        myTransform.position = startPos;
    }
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    public List<Character> combatants;
    public int currentTurnIndex = 0;

    [Header("Animation Settings")]
    public float attackMoveDistance = 1.5f; // How far characters move during attack
    public float attackAnimationDuration = 0.5f; // Duration of attack animation

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
        combatants = new List<Character>();

        // Assign teams
        foreach (Character c in playerTeam)
        {
            c.team = 0; // Player team
            combatants.Add(c);
        }

        foreach (Character c in enemyTeam)
        {
            c.team = 1; // Enemy team
            combatants.Add(c);
        }

        // Sort by speed (highest first)
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
            Character current = combatants[currentTurnIndex];

            if (current.state == "Dead")
            {
                NextTurn();
                continue;
            }

            current.BeginTurn(GetOpponentTeam(current));

            // Check if target is valid
            if (current.target != null)
            {
                float attackStartTime = Time.time;
                float attackDuration = current.data.attackDuration;

                // Start attack animation
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
                // No target available, skip turn
                Debug.Log($"{current.data.name} has no valid target. Skipping turn.");
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
            // Player can press buttons anytime during the attack duration
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
            // AI automatically chooses the best defense instantly
            defenseSucceeded = defender.AttemptDefense(attackStartTime, attackDuration, out defenseType);
            yield return null; // No waiting needed
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
        // Guard against null defender or data
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
                // Optional: counter damage calculation
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
}