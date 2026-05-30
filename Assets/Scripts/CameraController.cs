using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector2 leftBorder;
    public Vector2 rightBorder;
    public Camera cam;

    private SpawnPointRegistry registry;
    public ParallaxBackground background;
    public SpawnManager spawnManager;
    public PlayerController playerController;

    void Start()
    {
        cam = Camera.main;
        spawnManager = SpawnManager.Instance;
        playerController = PlayerController.Instance;
        registry = Object.FindFirstObjectByType<SpawnPointRegistry>();

        target = playerController.GetComponentInParent<Transform>();

        if (registry == null)
        {
            Debug.LogError("SpawnPointRegistry not found in the scene!");
        }

        changeBorders();
    }

    void LateUpdate()
    {
        camFollowPlayer();
    }

    public void SetBorders(Transform left, Transform right)
    {
        if (left != null) leftBorder = left.position;
        if (right != null) rightBorder = right.position;
    }

    public void changeBorders()
    {
        if (registry == null || spawnManager == null) return;

        var spawnPoint = registry.Get(spawnManager.currentAreaId);
        if (spawnPoint != null)
        {
            if (spawnPoint.leftBorder != null)
                leftBorder = spawnPoint.leftBorder.position;
            if (spawnPoint.rightBorder != null)
                rightBorder = spawnPoint.rightBorder.position;
        }
    }

    private void camFollowPlayer()
    {
        if (target == null) return;

        float halfCameraWidth = cam.orthographicSize * cam.aspect;
        Vector3 pos = transform.position;

        pos.x = target.position.x;
        pos.y = target.position.y + 1;

        float leftEdge = pos.x - halfCameraWidth;
        float rightEdge = pos.x + halfCameraWidth;

        if (leftEdge < leftBorder.x)
        {
            pos.x = leftBorder.x + halfCameraWidth;
            if (background != null) background.borderReached = true;
        }
        else if (rightEdge > rightBorder.x)
        {
            pos.x = rightBorder.x - halfCameraWidth;
            if (background != null) background.borderReached = true;
        }
        else
        {
            if (background != null) background.borderReached = false;
        }

        transform.position = pos;
    }
}