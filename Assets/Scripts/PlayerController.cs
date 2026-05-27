using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public Vector2 velocity;
    public Animator animator;
    public GameObject background;
    private ParallaxBackground backgroundScript;
    private SpawnManager spawnManager;

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private bool isBlocked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        backgroundScript = background.GetComponent<ParallaxBackground>();

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
        // Calculate actual movement
        Vector2 newPosition = rb.position + velocity * Time.fixedDeltaTime;
        Vector2 movementDelta = newPosition - lastPosition;

        // Apply movement
        rb.MovePosition(newPosition);

        // Only apply parallax if player actually moved (not blocked)
        if (movementDelta.magnitude > 0.1f)
        {
            backgroundScript.parallax(movementDelta);
        }

        lastPosition = rb.position;
    }

    void HandleInput()
    {
        velocity.x = 0;

        if (Input.GetKey(KeyCode.A))
            velocity.x = -speed;
        else if (Input.GetKey(KeyCode.D))
            velocity.x = speed;
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