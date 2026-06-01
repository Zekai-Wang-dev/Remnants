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

    void Awake()
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
        if (data == null) return;

        health = data.maxHealth;
        if (state == "Dead") state = "Alive";
        if (string.IsNullOrEmpty(state)) state = "Alive";
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

        // Default behavior if threatType is blank/unknown: attack first valid target.
        target = validTargets[0];
    }

    public bool AttemptDefense(Character attacker, float attackStartTime, float attackDuration, out string defenseType)
    {
        defenseType = "";
        bool defenseSucceeded = false;

        if (attacker == null || attacker.data == null || data == null) return false;

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
        if (attacker == null || attacker.data == null || data == null) return false;

        float parryChance = data.parryChance;
        float speedDiff = data.speed - attacker.data.speed;
        float accuracyDiff = data.accuracy - attacker.data.accuracy;

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
        if (attacker == null || attacker.data == null || data == null) return false;

        float dodgeChance = data.dodgeChance;
        float speedDiff = data.speed - attacker.data.speed;
        float accuracyDiff = data.accuracy - attacker.data.accuracy;

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
        if (attacker == null || attacker.data == null || data == null) return false;

        float blockChance = data.blockChance;
        float defenseDiff = data.defense - attacker.data.damage;

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
        if (targetTransform == null) yield break;

        // Important: use the current position when the attack begins.
        // Awake can run before CombatPreparation places the prefab on the combat grid.
        Vector3 startPos = myTransform.position;
        Vector3 targetPos = targetTransform.position;
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        Vector3 forwardPos = startPos + direction * (distance * 0.7f);

        float elapsed = 0f;
        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);

        while (elapsed < halfDuration)
        {
            myTransform.position = Vector3.Lerp(startPos, forwardPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        myTransform.position = forwardPos;

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

    public List<Character> combatants = new();
    public int currentTurnIndex = 0;

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
        StopAllCoroutines();
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
            if (c == null || c.data == null) continue;

            c.team = 0;
            c.state = "Alive";
            c.ResetHealth();
            combatants.Add(c);
        }

        foreach (Character c in enemyTeam)
        {
            if (c == null || c.data == null) continue;

            c.team = 1;
            c.state = "Alive";
            c.ResetHealth();
            combatants.Add(c);
        }

        combatants.Sort((a, b) => b.data.speed.CompareTo(a.data.speed));

        foreach (Character c in combatants)
        {
            c.threatLevel = 0;
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

            if (current == null || current.state == "Dead")
            {
                RemoveDeadCharacter(current);
                continue;
            }

            current.BeginTurn(GetOpponentTeam(current));

            if (current.target != null)
            {
                float attackDuration = current.data.attackDuration > 0f
                    ? current.data.attackDuration
                    : attackAnimationDuration;

                float attackStartTime = Time.time;

                StartCoroutine(current.AttackAnimation(current.target.transform, attackDuration));

                bool defenseSucceeded = false;
                string defenseType = "";

                yield return StartCoroutine(HandleDefenseWindow(
                    current,
                    current.target,
                    attackStartTime,
                    attackDuration,
                    (success, type) =>
                    {
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

            bool playerTeamAlive = false;
            bool enemyTeamAlive = false;

            foreach (Character c in combatants)
            {
                if (c == null || c.state == "Dead") continue;
                if (c.team == 0) playerTeamAlive = true;
                if (c.team == 1) enemyTeamAlive = true;
            }

            if (!enemyTeamAlive)
            {
                yield return StartCoroutine(HandleVictory());
                yield break;
            }

            if (!playerTeamAlive)
            {
                Debug.Log("Player team defeated!");
                yield break;
            }

            NextTurn();
            yield return new WaitForSeconds(1f);
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

        if (attacker == null || defender == null)
        {
            onComplete?.Invoke(false, "");
            yield break;
        }

        if (defender.isPlayer)
        {
            while (Time.time - attackStartTime < attackDuration && !defenseSucceeded)
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

                yield return null;
            }
        }
        else
        {
            defenseSucceeded = defender.AttemptDefense(attacker, attackStartTime, attackDuration, out defenseType);
            yield return null;
        }

        defender.lastDefenseUsed = defenseType;
        onComplete?.Invoke(defenseSucceeded, defenseType);
    }

    private void ApplyDefenseResults(Character attacker, Character defender, string defenseType)
    {
        if (attacker == null || defender == null) return;

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
        if (attacker == null || attacker.data == null || defender == null || defender.data == null) return;
        if (attacker.state == "Dead" || defender.state == "Dead") return;

        float damage = attacker.data.damage - defender.data.defense;
        damage = Mathf.Max(damage, 1f);

        switch (defenseContext)
        {
            case "Blocked":
                damage *= 0.5f;
                break;

            case "Counter":
                // Current behavior: parry avoids incoming damage and deals defender's attack damage back.
                damage = Mathf.Max(attacker.data.damage - defender.data.defense, 1f);
                break;

            case "Dodge":
                damage = 0f;
                break;
        }

        if (damage <= 0f) return;

        defender.health -= damage;
        Debug.Log($"{attacker.data.name} dealt {damage} damage to {defender.data.name}");

        if (defender.health <= 0f)
        {
            defender.state = "Dead";
            Debug.Log($"{defender.data.name} died!");
        }
    }

    private IEnumerator HandleVictory()
    {
        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager != null)
        {
            string encounterId = spawnManager.currentEncounterId;
            string encounterAreaId = spawnManager.currentEncounterAreaId;

            // Random encounters should not permanently remove fixed world encounters.
            bool isRandomEncounter = !string.IsNullOrEmpty(encounterId) && encounterId.StartsWith("random_");

            if (!isRandomEncounter &&
                !string.IsNullOrEmpty(encounterAreaId) &&
                !string.IsNullOrEmpty(encounterId))
            {
                spawnManager.MarkEncounterDefeated(encounterAreaId, encounterId);
            }

            Debug.Log($"Victory! Waiting {bufferTimePostCombat} seconds before returning...");
            yield return new WaitForSeconds(bufferTimePostCombat);

            spawnManager.ClearCurrentEncounter();
            spawnManager.NextScene("CombatOver");
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

        int removedIndex = combatants.IndexOf(deadCharacter);
        combatants.Remove(deadCharacter);

        if (removedIndex >= 0 && removedIndex < currentTurnIndex)
        {
            currentTurnIndex--;
        }

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
            if (combatants[i] == null || combatants[i].state == "Dead")
            {
                RemoveDeadCharacter(combatants[i]);
            }
        }
    }
}
