using UnityEngine;

public class CinematicsActivator : MonoBehaviour
{
    public SceneData sceneData;
    public bool destroyAfterUse = true;

    private void Start()
    {
        if (ShouldBeDisabledBecauseAlreadyActivated())
        {
            DisableOrDestroyActivator();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (sceneData == null)
        {
            Debug.LogWarning($"{name} has no SceneData assigned.");
            return;
        }

        if (FilmSceneManager.Instance == null)
        {
            Debug.LogError("FilmSceneManager is missing from the scene.");
            return;
        }

        if (FilmSceneManager.Instance.IsCinematicActivated(sceneData))
        {
            DisableOrDestroyActivator();
            return;
        }

        bool started = FilmSceneManager.Instance.TryStartCinematic(sceneData);

        if (!started)
        {
            return;
        }

        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
    }

    private bool ShouldBeDisabledBecauseAlreadyActivated()
    {
        if (sceneData == null)
        {
            return false;
        }

        if (!sceneData.isPermanent)
        {
            return false;
        }

        if (FilmSceneManager.Instance == null)
        {
            return sceneData.isActivated;
        }

        return FilmSceneManager.Instance.IsCinematicActivated(sceneData);
    }

    private void DisableOrDestroyActivator()
    {
        if (destroyAfterUse)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        enabled = false;
    }
}