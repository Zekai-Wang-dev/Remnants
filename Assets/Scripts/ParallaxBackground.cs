using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParallaxArea
{
    public Vector2 areaIndex;
    public float layer1Speed;
    public float layer2Speed;
    public float layer3Speed;
    public List<GameObject> backgroundLayers1;
    public List<GameObject> backgroundLayers2;
    public List<GameObject> backgroundLayers3;
}

public class ParallaxBackground : MonoBehaviour
{
    public bool borderReached = false;
    public List<ParallaxArea> parallaxAreas;
    public ParallaxArea currentParallaxArea;

    public void Start()
    {
        SpawnManager spawnManager = SpawnManager.Instance;
        if (spawnManager != null && parallaxAreas != null)
        {
            currentParallaxArea = parallaxAreas.Find(area => area.areaIndex == spawnManager.currentAreaIndex);
        }
    }

    public void parallax(Vector2 plrVelocity)
    {
        if (borderReached || currentParallaxArea == null) return;

        if (currentParallaxArea.backgroundLayers1 != null)
        {
            for (int i = 0; i < currentParallaxArea.backgroundLayers1.Count; i++)
            {
                currentParallaxArea.backgroundLayers1[i].transform.Translate(plrVelocity.x / currentParallaxArea.layer1Speed * Time.deltaTime, 0, 0);
            }
        }

        if (currentParallaxArea.backgroundLayers2 != null)
        {
            for (int i = 0; i < currentParallaxArea.backgroundLayers2.Count; i++)
            {
                currentParallaxArea.backgroundLayers2[i].transform.Translate(plrVelocity.x / currentParallaxArea.layer2Speed * Time.deltaTime, 0, 0);
            }
        }

        if (currentParallaxArea.backgroundLayers3 != null)
        {
            for (int i = 0; i < currentParallaxArea.backgroundLayers3.Count; i++)
            {
                currentParallaxArea.backgroundLayers3[i].transform.Translate(plrVelocity.x / currentParallaxArea.layer3Speed * Time.deltaTime, 0, 0);
            }
        }
    }
}