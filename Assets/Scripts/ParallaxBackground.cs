using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParallaxArea 
{
    public Vector2 areaIndex;

    public float layer1Speed;
    public float layer2Speed;
    public float layer3Speed;

    public List<GameObject> backgroundLayers1; // List of background layers for parallax effect
    public List<GameObject> backgroundLayers2; // List of background layers for parallax effect
    public List<GameObject> backgroundLayers3; // List of background layers for parallax effect
}

public class ParallaxBackground : MonoBehaviour
{

    public bool borderReached = false; // Flag to indicate if the player camera has reached the border
    public List<ParallaxArea> parallaxAreas; // List of parallax areas with their layers and speeds
    public ParallaxArea currentParallaxArea; // Current parallax area with its layers and speeds

    public void Start()
    {
        SpawnManager spawnManager = SpawnManager.Instance;
        currentParallaxArea = parallaxAreas.Find(area => area.areaIndex == spawnManager.currentAreaIndex);

    }

    public void parallax(Vector2 plrVelocity)
    {

        if (borderReached) return;

        for (int i = 0; i < currentParallaxArea.backgroundLayers1.Count; i++)
        {
            currentParallaxArea.backgroundLayers1[i].transform.Translate(plrVelocity.x / currentParallaxArea.layer1Speed * Time.deltaTime, 0, 0); // Move the first layer based on player velocity
        }
        for (int i = 0; i < currentParallaxArea.backgroundLayers2.Count; i++)
        {
            currentParallaxArea.backgroundLayers2[i].transform.Translate(plrVelocity.x / currentParallaxArea.layer2Speed * Time.deltaTime, 0, 0); // Move the second layer based on player velocity
        }
        for (int i = 0; i < currentParallaxArea.backgroundLayers3.Count; i++)
        {
            currentParallaxArea.backgroundLayers3[i].transform.Translate(plrVelocity.x / currentParallaxArea.layer3Speed * Time.deltaTime, 0, 0); // Move the third layer based on player velocity
        }
    }

}
