using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

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

    public float InteractRadius;
    public float ExitRadius;

    public float areaTransitionCooldown = 0.5f;
    private float lastTransitionTime = -10f;
    private bool isTransitioning = false;

    public AreaData currentArea;

    // ✅ NEW: Throttle random encounters
    private float randomCheckInterval = 0.5f; // Check every 0.5 seconds
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

        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        ResetDistanceTracking();
    }

    void Update()
    {
        HandleInput();
        UpdateAnimations();

        // ✅ Only check for random encounters at intervals
        if (Time.time >= nextRandomCheckTime)
        {
            CheckForRandomEncounters();
            nextRandomCheckTime = Time.time + randomCheckInterval;
        }
    }

    void FixedUpdate()
    {
        Vector2 newPosition = rb.position + velocity * Time.fixedDeltaTime;
        Vector2 movementDelta = newPosition - lastPosition;

        rb.MovePosition(newPosition);

        if (movementDelta.magnitude > 0.1f && backgroundScript != null)
        {
            backgroundScript.parallax(velocity);
        }

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
        lastPosition = rb.position;
        accumulatedDistance = 0f;
        Debug.Log("Distance tracking reset");
    }

    private void GetCurrentArea()
    {
        currentArea = spawnManager.GetCurrentAreaData();
    }
    void CheckForRandomEncounters()
    {
        if (spawnManager.currentSceneId == "001") return;

        AreaData area = spawnManager.GetCurrentAreaData();
        if (area == null || !area.enableRandomEncounters) return; 

        // ✅ Only check for encounter if we've moved enough distance
        if (accumulatedDistance < area.minDistanceBetweenEncounters) return;

        float roll = Random.value;
        Debug.Log($"Rolled: {roll:F3}, Needed: {area.randomEncounterChance:F3}");

        if (roll < area.randomEncounterChance)
        {
            Debug.Log("✅ Random encounter triggered!");

            CharacterData enemy = area.PickRandomEnemy();
            spawnManager.SetCurrentEncounter(area.areaId, "random_" + enemy.name);

            accumulatedDistance = 0f;
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
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(ExitRadius, ExitRadius), 0f, LayerMask.GetMask("Exit"));

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(direction))
            {
                ResetDistanceTracking();
                spawnManager.NextArea(direction);
                print("Exit " + direction + " detected, changing area...");
                GetCurrentArea();
                ResetDistanceTracking();

            }
        }
    }

    void CheckForEncounters()
    {
        if (spawnManager.currentSceneId == "001") return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, InteractRadius, LayerMask.GetMask("Encounter"));

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Encounter"))
            {
                EncounterTrigger trigger = hit.GetComponent<EncounterTrigger>();
                if (trigger != null)
                {
                    spawnManager.SetCurrentEncounter(trigger.areaId, trigger.encounterId);
                }

                print("Enemy detected, triggering encounter...");
                Destroy(hit.gameObject);
                ResetDistanceTracking();
                spawnManager.NextScene("Combat");
            }
        }
    }

    void HandleInput()
    {

        velocity.x = 0;
        if (spawnManager.currentSceneId == "001") return;


        if (Input.GetKey(KeyCode.A))
            velocity.x = -speed;
        else if (Input.GetKey(KeyCode.D))
            velocity.x = speed;
        else if (Input.GetKeyDown(KeyCode.W))
            CheckForExits("Forward");
        else if (Input.GetKeyDown(KeyCode.S))
            CheckForExits("Backward");
    }

    void UpdateAnimations()
    {
        bool isWalking = velocity.magnitude > 0 && !isBlocked;
        animator.SetBool("walking", isWalking);
        animator.SetFloat("xvelocity", velocity.x);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) return;

        isBlocked = true;
        velocity = Vector2.zero;

        if (Time.time - lastTransitionTime < areaTransitionCooldown) return;

        if (collision.gameObject.CompareTag("LeftArea"))
        {
            if (velocity.x < 0 || Input.GetKey(KeyCode.A) || transform.position.x > collision.transform.position.x)
            {
                ExecuteAreaTransition("Left", collision);
            }
        }
        else if (collision.gameObject.CompareTag("RightArea"))
        {
            if (velocity.x > 0 || Input.GetKey(KeyCode.D) || transform.position.x < collision.transform.position.x)
            {
                ExecuteAreaTransition("Right", collision);
            }
        }
    }

    void ExecuteAreaTransition(string direction, Collision2D collision)
    {
        Vector2 offset = Vector2.zero;
        if (direction == "Left")
            offset = new Vector2(-2f, 0f);
        else if (direction == "Right")
            offset = new Vector2(2f, 0f);

        rb.MovePosition(rb.position + offset);
        SpawnManager.Instance.NextArea(direction);

        ResetDistanceTracking();

        lastTransitionTime = Time.time;
        isTransitioning = true;
        GetCurrentArea();

        print($"Transitioned {direction} with offset");
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