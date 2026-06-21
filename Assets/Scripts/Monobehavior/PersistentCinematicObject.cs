using System.Collections;
using UnityEngine;

public class PersistentCinematicObject : MonoBehaviour
{
    [Header("Hide/Destroy When This Cinematic Is Activated")]
    public SceneData hideIfCinematicActivated;

    [Header("Behavior")]
    public bool disableWhenActivated = true;
    public bool destroyWhenActivated = false;

    private void Start()
    {
        StartCoroutine(RefreshAfterSaveLoaded());
    }

    private IEnumerator RefreshAfterSaveLoaded()
    {
        // Wait one frame so managers can initialize.
        yield return null;

        // Wait until save data is loaded.
        while (GameSaveManager.Instance != null && !GameSaveManager.Instance.HasLoaded)
        {
            yield return null;
        }

        RefreshCinematicObjectState();
    }

    public void RefreshCinematicObjectState()
    {
        if (hideIfCinematicActivated == null)
        {
            return;
        }

        bool shouldHide = false;

        if (FilmSceneManager.Instance != null)
        {
            shouldHide = FilmSceneManager.Instance.IsCinematicActivated(hideIfCinematicActivated);
        }
        else
        {
            shouldHide = hideIfCinematicActivated.isActivated;
        }

        if (!shouldHide)
        {
            return;
        }

        if (destroyWhenActivated)
        {
            Destroy(gameObject);
            return;
        }

        if (disableWhenActivated)
        {
            gameObject.SetActive(false);
        }
    }
}