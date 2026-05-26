using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{

    public bool borderReached = false; // Flag to indicate if the player camera has reached the border
    public float layer1Speed; 
    public float layer2Speed;
    public float layer3Speed;

    public List<GameObject> backgroundLayers1; // List of background layers for parallax effect
    public List<GameObject> backgroundLayers2; // List of background layers for parallax effect
    public List<GameObject> backgroundLayers3; // List of background layers for parallax effect

    public void parallax(Vector2 plrVelocity)
    {

        if (borderReached) return;

        for (int i = 0; i < backgroundLayers1.Count; i++)
        {
            backgroundLayers1[i].transform.Translate(plrVelocity.x / layer1Speed * Time.deltaTime, 0, 0); // Move the first layer based on player velocity
        }
        for (int i = 0; i < backgroundLayers2.Count; i++)
        {
            backgroundLayers2[i].transform.Translate(plrVelocity.x / layer2Speed * Time.deltaTime, 0, 0); // Move the second layer based on player velocity
        }
        for (int i = 0; i < backgroundLayers3.Count; i++)
        {
            backgroundLayers3[i].transform.Translate(plrVelocity.x / layer3Speed * Time.deltaTime, 0, 0); // Move the third layer based on player velocity
        }
    }

}
