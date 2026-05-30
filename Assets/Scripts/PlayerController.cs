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
        lastPosition = rb.position;
    }

    void Update()
    {
        HandleInput();
        UpdateAnimations();
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

        lastPosition = rb.position;
        CheckForEncounters();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector2(ExitRadius, ExitRadius));
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
                spawnManager.NextArea(direction);
                print("Exit " + direction + " detected, changing area...");
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
                    // Store the encounter info for when we return from combat
                    spawnManager.SetCurrentEncounter(trigger.areaId, trigger.encounterId);
                }

                print("Enemy detected, triggering encounter...");
                Destroy(hit.gameObject);
                spawnManager.NextScene("Combat");
            }
        }
    }

    void HandleInput()
    {
        velocity.x = 0;

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

        if (collision.gameObject.CompareTag("LeftArea"))
        {
            SpawnManager.Instance.NextArea("Left");
        }
        else if (collision.gameObject.CompareTag("RightArea"))
        {
            SpawnManager.Instance.NextArea("Right");
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
        {
            isBlocked = false;
        }
    }
}