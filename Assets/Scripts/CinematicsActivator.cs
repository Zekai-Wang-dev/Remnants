using UnityEngine;
using UnityEngine.SceneManagement;

public class CinematicsActivator : MonoBehaviour
{

    public SceneData sceneData;
    private FilmSceneManager sceneManager = FilmSceneManager.Instance; 

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        PlayerController.Instance.DisableMovement();
        sceneManager.StartCinematic(sceneData);
        Destroy(gameObject);

    }

}
