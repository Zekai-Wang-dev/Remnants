using UnityEngine;

public class CameraController : MonoBehaviour
{

    public Transform target;

    public GameObject leftBorder; 
    public GameObject rightBorder;

    public Camera cam;

    public float halfCameraWidth; 

    public ParallaxBackground background;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        cam = Camera.main;
        halfCameraWidth = cam.orthographicSize * cam.aspect; // Calculate half of the camera's width based on its orthographic size and aspect ratio

    }

    void LateUpdate() // Use LateUpdate for camera follow
    {
        camFollowPlayer();
    }

    private void camFollowPlayer()
    {
        // 1. Calculate the desired X position first (where the camera WANTS to go)
        // Do NOT read transform.position here yet for the target calculation
        float desiredX = target.position.x;

        // 2. Calculate the camera's half-width ONCE using the CURRENT camera position
        // This is crucial: we need to know the current view size to clamp correctly
        float halfCameraWidth = cam.orthographicSize * cam.aspect;

        // 3. Get the current camera position to use as the base for clamping
        Vector3 currentPos = transform.position;

        // 4. Calculate the proposed new edges based on the DESIRED X position
        // This checks: "If I go to this new X, will my edges cross the border?"
        float proposedLeftEdge = desiredX - halfCameraWidth;
        float proposedRightEdge = desiredX + halfCameraWidth;

        background.borderReached = false; // Reset the border reached flag before clamping

        // 5. Clamp the desired X position
        if (proposedLeftEdge < leftBorder.transform.position.x)
        {
            // If the left edge is too far left, push the desired X to the right
            desiredX = leftBorder.transform.position.x + halfCameraWidth;
            background.borderReached = true;
        }
        else if (proposedRightEdge > rightBorder.transform.position.x)
        {
            // If the right edge is too far right, push the desired X to the left
            desiredX = rightBorder.transform.position.x - halfCameraWidth;
            background.borderReached = true;
        }

        // 6. Build the final position ONCE
        Vector3 newPos = transform.position;
        newPos.x = desiredX;

        // Optional: Keep Y/Z if needed
        // newPos.y = ...;

        // 7. Apply the position ONCE at the very end
        transform.position = newPos;
    }
}
