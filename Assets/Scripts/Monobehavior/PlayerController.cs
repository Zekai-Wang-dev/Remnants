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
    private bool canMove = true; 

    public float InteractRadius;
    public float ExitRadius;

    public float areaTransitionCooldown = 0.5f;
    private float lastTransitionTime = -10f;
    private bool isTransitioning = false;

    public AreaData currentArea;

    private float randomCheckInterval = 0.5f; // Check every 0.5 seconds.
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

        Debug.Log(velocity); 
        accumulatedDistance += movementDelta.magnitude;
        lastPosition = rb.position;

        CheckForEncounters();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector2(ExitRadius, ExitRadius));
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
    }

    public void EnableMovement()
    {
        canMove = true;
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
                spawnManager.NextArea(direction);
                Debug.Log("Exit " + direction + " detected, changing area...");
                GetCurrentArea();
                ResetDistanceTracking();
            }
        }
    }

    void CheckForEncounters()
    {
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
        if (!canMove) return;
        if (spawnManager.currentSceneId == "001") return;

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
