using System.Collections;
using UnityEngine;

public class PersistentCinematicObject : MonoBehaviour
{
    public enum CompletedCinematicBehavior
    {
        None,
        Disable,
        Destroy
    }

    [Header("Cinematic Condition")]
    [Tooltip("The permanent cinematic that controls whether this object should remain in the world.")]
    public SceneData cinematicSceneData;

    [Header("Behavior")]
    public CompletedCinematicBehavior behaviorWhenCinematicCompleted =
        CompletedCinematicBehavior.Destroy;

    [Tooltip("If true, waits until GameSaveManager has loaded before checking cinematic state.")]
    public bool waitForSaveLoad = true;

    private bool hasRefreshedAfterLoad;

    private void Start()
    {
        StartCoroutine(RefreshWhenReady());
    }

    private IEnumerator RefreshWhenReady()
    {
        yield return null;

        if (waitForSaveLoad)
        {
            yield return new WaitUntil(() =>
                GameSaveManager.Instance == null ||
                GameSaveManager.Instance.HasLoaded
            );
        }

        yield return null;

        hasRefreshedAfterLoad = true;
        RefreshCinematicObjectState();
    }

    public void RefreshCinematicObjectState()
    {
        if (cinematicSceneData == null)
        {
            return;
        }

        if (FilmSceneManager.Instance == null)
        {
            return;
        }

        if (!FilmSceneManager.Instance.IsCinematicActivated(cinematicSceneData))
        {
            return;
        }

        switch (behaviorWhenCinematicCompleted)
        {
            case CompletedCinematicBehavior.None:
                break;

            case CompletedCinematicBehavior.Disable:
                gameObject.SetActive(false);
                break;

            case CompletedCinematicBehavior.Destroy:
                Destroy(gameObject);
                break;
        }
    }
}