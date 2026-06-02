using UnityEngine;
using UnityEngine.SceneManagement;

public class CinematicsActivator : MonoBehaviour
{

    public SceneData sceneData;
    private FilmSceneManager sceneManager = FilmSceneManager.Instance; 

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        sceneManager.StartCinematic(sceneData);
        Destroy(gameObject);

    }

}
