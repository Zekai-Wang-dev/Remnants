using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

public class PlayerController : MonoBehaviour
{

    public float speed = 5f; // Speed of the player movement
    public Vector2 velocity; // Store the movement input

    public Animator animator; // Reference to the Animator component for handling animations

    public bool isWalking = false;

    public GameObject background; // Reference to the background for parallax effect
    private ParallaxBackground backgroundScript; // Reference to the ParallaxBackground script to call the parallax function

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        backgroundScript = background.GetComponent<ParallaxBackground>();

    }

    // Update is called once per frame
    void Update()
    {

        movement(); // Call the movement function to handle player movement each frame

    }

    private void movement()
    {

        if (Input.GetKey(KeyCode.A))
        {
            velocity.x = -speed;

        }
        else if (Input.GetKey(KeyCode.D))
        {
            velocity.x = speed;

        }
        else velocity.x = 0;

        if (velocity.magnitude > 0) isWalking = true;

        else isWalking = false;

        animator.SetBool("walking", isWalking);

        animator.SetFloat("xvelocity", velocity.x); // Set the xvelocity parameter in the Animator to control animations based on horizontal movement

        transform.Translate(velocity * Time.deltaTime); // Move the player based on velocity and frame time

        backgroundScript.parallax(velocity, speed); // Call the parallax function in the background script to create a parallax effect based on player movement

    }

}
