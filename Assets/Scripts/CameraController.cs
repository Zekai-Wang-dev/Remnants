using UnityEngine;

public class CameraController : MonoBehaviour
{

    public Transform target;

    public GameObject leftBorder; 
    public GameObject rightBorder;

    public Camera cam;

    public ParallaxBackground background;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        cam = Camera.main;

    }

    void LateUpdate() // Use LateUpdate for camera follow
    {
        camFollowPlayer();
    }

    private void camFollowPlayer()
    {

        float halfCameraWidth = cam.orthographicSize * cam.aspect;

        Vector3 pos = transform.position;
        pos.x = target.position.x;

        float leftEdge = pos.x - halfCameraWidth;
        float rightEdge = pos.x + halfCameraWidth;

        if (leftEdge < leftBorder.transform.position.x)
        {
            pos.x = leftBorder.transform.position.x + halfCameraWidth;
            background.borderReached = true;
        }
        else if (rightEdge > rightBorder.transform.position.x)
        {
            pos.x = rightBorder.transform.position.x - halfCameraWidth;
            background.borderReached = true;
        }
        else
        {
            background.borderReached = false; // Reset the flag if we're within bounds
        }

        transform.position = pos;

    }

}
