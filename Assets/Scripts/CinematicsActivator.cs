using UnityEngine;

public class CinematicsActivator : MonoBehaviour
{
    public SceneData sceneData;
    public bool destroyAfterUse = true;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (FilmSceneManager.Instance == null)
        {
            Debug.LogError("FilmSceneManager is missing from the scene.");
            return;
        }

        FilmSceneManager.Instance.StartCinematic(sceneData);

        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
    }
}
