using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{

    public bool borderReached = false; // Flag to indicate if the player camera has reached the border
    public float borderSlowed; 

    public void parallax(Vector2 plrVelocity, float plrSpeed)
    {

        if (borderReached) return;

        transform.Translate(plrVelocity.x/borderSlowed * Time.deltaTime, 0, 0); // Move the background based on player velocity

    }


}
