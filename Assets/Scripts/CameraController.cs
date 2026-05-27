using UnityEngine;

public class CameraController : MonoBehaviour
{

    public Transform target;

    public Vector2 leftBorder; 
    public Vector2 rightBorder;

    public Camera cam;

    public ParallaxBackground background;
    public SpawnManager spawnManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        cam = Camera.main;
        spawnManager = SpawnManager.Instance;
        changeBorders();

    }

    void LateUpdate() // Use LateUpdate for camera follow
    {
        camFollowPlayer();
    }

    public void changeBorders()
    {

        Vector2 newLeftBorder = spawnManager.scenes[spawnManager.FindSceneIndex(spawnManager.currentSceneId)].areas[spawnManager.FindAreaIndex(spawnManager.currentAreaId)].leftBorder;
        Vector2 newRightBorder = spawnManager.scenes[spawnManager.FindSceneIndex(spawnManager.currentSceneId)].areas[spawnManager.FindAreaIndex(spawnManager.currentAreaId)].rightBorder;

        leftBorder = newLeftBorder;
        rightBorder = newRightBorder;      
        transform.position = new Vector3(target.position.x, transform.position.y, transform.position.z); // Move camera to the new position immediately

    }

    private void camFollowPlayer()
    {

        if (leftBorder == null || rightBorder == null) return; // Ensure borders are assigned   

        float halfCameraWidth = cam.orthographicSize * cam.aspect;

        Vector3 pos = transform.position;

        pos.x = target.position.x;

        float leftEdge = pos.x - halfCameraWidth;
        float rightEdge = pos.x + halfCameraWidth;

        if (leftEdge < leftBorder.x)
        {
            pos.x = leftBorder.x + halfCameraWidth;
            background.borderReached = true;
        }
        else if (rightEdge > rightBorder.x)
        {
            pos.x = rightBorder.x - halfCameraWidth;
            background.borderReached = true;
        }
        else
        {
            background.borderReached = false; // Reset the flag if we're within bounds
        }

        transform.position = pos;

    }

}
