using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public Vector2 velocity;
    public Animator animator;
    public ParallaxBackground backgroundScript;
    private SpawnManager spawnManager;

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private bool isBlocked = false;
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool movementDisabledByCombat = false;

    public bool CanMove => canMove;
    public bool MovementDisabledByCombat => movementDisabledByCombat;

    [Header("Interaction / Detection")]
    public float InteractRadius;
    public float ExitRadius;

    private WorldInteractable currentHighlightedInteractable;

    [Header("Area Transition")]
    public float areaTransitionCooldown = 0.5f;
    private float lastTransitionTime = -10f;
    private bool isTransitioning = false;

    public AreaData currentArea;

    [Header("Random Encounters")]
    private float randomCheckInterval = 0.5f;
    private float nextRandomCheckTime = 0f;
    public float accumulatedDistance;

    public static PlayerController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnManager = SpawnManager.Instance;

        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody2D.");
            enabled = false;
            return;
        }

        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager not found.");
            enabled = false;
            return;
        }

        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GetCurrentArea();
        ResetDistanceTracking();
    }

    void Update()
    {
        HandleInput();
        UpdateAnimations();

        if (Time.time >= nextRandomCheckTime)
        {
            CheckForRandomEncounters();
            nextRandomCheckTime = Time.time + randomCheckInterval;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 newPosition = rb.position + velocity * Time.fixedDeltaTime;
        Vector2 movementDelta = newPosition - lastPosition;

        rb.MovePosition(newPosition);

        if (movementDelta.magnitude > 0.1f && backgroundScript != null)
        {
            backgroundScript.parallax(velocity);
        }

        accumulatedDistance += movementDelta.magnitude;
        lastPosition = rb.position;

        if (!canMove)
        {
            return;
        }

        CheckForEncounters();
        CheckForInteractables();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector2(ExitRadius, ExitRadius));

        Gizmos.DrawWireSphere(transform.position, InteractRadius);
    }

    void ResetDistanceTracking()
    {
        if (rb != null)
        {
            lastPosition = rb.position;
        }

        accumulatedDistance = 0f;
    }

    private void GetCurrentArea()
    {
        if (spawnManager == null) return;
        currentArea = spawnManager.GetCurrentAreaData();
    }

    public void DisableMovement()
    {
        velocity = Vector2.zero;
        canMove = false;
        ClearHighlightedInteractable();
    }

    public void DisableMovementForCombat()
    {
        movementDisabledByCombat = true;
        DisableMovement();
    }

    public void EnableMovement()
    {
        movementDisabledByCombat = false;
        ForceEnableMovement("EnableMovement");
    }

    public void EnableMovementAfterCombat()
    {
        movementDisabledByCombat = false;
        ForceEnableMovement("CombatOver");
    }

    public void ForceEnableMovement(string reason = "Manual")
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (spawnManager == null)
        {
            spawnManager = SpawnManager.Instance;
        }

        canMove = true;
        isBlocked = false;
        isTransitioning = false;
        velocity = Vector2.zero;

        GetCurrentArea();
        ResetDistanceTracking();
        ClearHighlightedInteractable();

        nextRandomCheckTime = Time.time + randomCheckInterval;

        Debug.Log($"Player movement enabled. Reason: {reason}. Current scene: {(spawnManager != null ? spawnManager.currentSceneId : "No SpawnManager")}");
    }

    void CheckForRandomEncounters()
    {
        if (!canMove) return;
        if (spawnManager == null) return;
        if (spawnManager.currentSceneId == "001") return;

        AreaData area = spawnManager.GetCurrentAreaData();
        currentArea = area;

        if (area == null || !area.enableRandomEncounters) return;

        if (accumulatedDistance < area.minDistanceBetweenEncounters) return;

        float roll = Random.value;

        if (roll < area.randomEncounterChance)
        {
            CharacterData enemy = area.PickRandomEnemy();

            if (enemy == null)
            {
                Debug.LogWarning($"Random encounter rolled in area '{area.areaId}', but no enemy could be selected.");
                ResetDistanceTracking();
                return;
            }

            Debug.Log($"Random encounter triggered: {enemy.name}");

            ClearHighlightedInteractable();

            spawnManager.SetCurrentEncounter(area.areaId, "random_" + enemy.name);
            ResetDistanceTracking();
            spawnManager.NextScene("Combat");
        }
    }

    public void FindBgScript(ParallaxBackground bg)
    {
        if (bg == null) return;
        backgroundScript = bg;
    }

    void CheckForExits(string direction)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,
            new Vector2(ExitRadius, ExitRadius),
            0f,
            LayerMask.GetMask("Exit"));

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(direction))
            {
                ClearHighlightedInteractable();

                spawnManager.NextArea(direction);
                Debug.Log("Exit " + direction + " detected, changing area...");

                GetCurrentArea();
                ResetDistanceTracking();
            }
        }
    }

    void CheckForInteractables()
    {
        if (spawnManager == null)
        {
            ClearHighlightedInteractable();
            Debug.LogError("SpawnManager not found.");
            return;
        }

        if (spawnManager.currentSceneId == "001")
        {
            ClearHighlightedInteractable();
            Debug.Log("In scene 001, skipping interactable check.");
            return;
        }

        if (!canMove)
        {
            ClearHighlightedInteractable();
            Debug.Log("Movement disabled, skipping interactable check.");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            InteractRadius,
            LayerMask.GetMask("Interactable"));

        WorldInteractable closestInteractable = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {

            if (!hit.CompareTag("Interactable"))
            {
                Debug.LogWarning("Collider detected in Interactable layer that does not have Interactable tag.");
                continue;
            }

            Debug.Log($"Found interactable candidate: {hit.name}");

            WorldInteractable interactable = hit.GetComponent<WorldInteractable>();

            if (interactable == null)
            {
                Debug.LogWarning("Interactable collider has Interactable tag but no WorldInteractable component.");
                continue;
            }

            float distanceSqr = ((Vector2)hit.transform.position - rb.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestInteractable = interactable;
            }
        }

        if (currentHighlightedInteractable == closestInteractable)
        {
            return;
        }

        if (currentHighlightedInteractable != null)
        {
            currentHighlightedInteractable.Unhighlight();
        }

        currentHighlightedInteractable = closestInteractable;

        if (currentHighlightedInteractable != null)
        {
            currentHighlightedInteractable.Highlight();
        }
    }

    private void ClearHighlightedInteractable()
    {
        if (currentHighlightedInteractable != null)
        {
            currentHighlightedInteractable.Unhighlight();
            currentHighlightedInteractable = null;
        }
    }

    void CheckForEncounters()
    {
        if (!canMove) return;
        if (spawnManager == null) return;
        if (spawnManager.currentSceneId == "001") return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            InteractRadius,
            LayerMask.GetMask("Encounter"));

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Encounter"))
            {
                EncounterTrigger trigger = hit.GetComponent<EncounterTrigger>();

                if (trigger != null)
                {
                    spawnManager.SetCurrentEncounter(trigger.areaId, trigger.encounterId);
                }
                else
                {
                    Debug.LogWarning("Encounter collider has Encounter tag but no EncounterTrigger component.");
                }

                Debug.Log("Enemy detected, triggering encounter...");

                ClearHighlightedInteractable();

                Destroy(hit.gameObject);
                ResetDistanceTracking();

                spawnManager.NextScene("Combat");
                return;
            }
        }
    }

    void HandleInput()
    {
        velocity.x = 0;

        if (spawnManager == null) return;

        if (!canMove)
        {
            ClearHighlightedInteractable();
            return;
        }

        if (spawnManager.currentSceneId == "001")
        {
            ClearHighlightedInteractable();
            return;
        }

        if (Input.GetKey(KeyCode.A))
        {
            velocity.x = -speed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            velocity.x = speed;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            CheckForExits("Forward");
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            CheckForExits("Backward");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentHighlightedInteractable != null)
            {
                currentHighlightedInteractable.Interact();

                // The object may destroy itself after interaction.
                currentHighlightedInteractable = null;
            }
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        bool isWalking = velocity.magnitude > 0 && !isBlocked;
        animator.SetBool("walking", isWalking);
        animator.SetFloat("xvelocity", velocity.x);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) return;

        float incomingX = velocity.x;
        bool pressingLeft = Input.GetKey(KeyCode.A);
        bool pressingRight = Input.GetKey(KeyCode.D);

        isBlocked = true;
        velocity = Vector2.zero;

        if (Time.time - lastTransitionTime < areaTransitionCooldown) return;

        if (collision.gameObject.CompareTag("LeftArea"))
        {
            if (incomingX < 0 || pressingLeft || transform.position.x > collision.transform.position.x)
            {
                ExecuteAreaTransition("Left");
            }
        }
        else if (collision.gameObject.CompareTag("RightArea"))
        {
            if (incomingX > 0 || pressingRight || transform.position.x < collision.transform.position.x)
            {
                ExecuteAreaTransition("Right");
            }
        }
    }

    void ExecuteAreaTransition(string direction)
    {
        ClearHighlightedInteractable();

        Vector2 offset = Vector2.zero;

        if (direction == "Left")
        {
            offset = new Vector2(-2f, 0f);
        }
        else if (direction == "Right")
        {
            offset = new Vector2(2f, 0f);
        }

        rb.MovePosition(rb.position + offset);
        spawnManager.NextArea(direction);

        ResetDistanceTracking();

        lastTransitionTime = Time.time;
        isTransitioning = true;
        GetCurrentArea();

        Debug.Log($"Transitioned {direction} with offset");
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
        {
            isBlocked = false;
            isTransitioning = false;
        }
    }
}