using System.Collections;
using UnityEngine;

public class CinematicsActivator : MonoBehaviour
{
    public SceneData sceneData;
    public bool destroyAfterUse = true;

    [Header("Opening / Auto Start")]
    [Tooltip("Use this for the demo opening cinematic instead of relying on the player spawning inside a trigger.")]
    public bool startOnSceneStart = false;

    [Tooltip("Prevents the cinematic from starting before GameSaveManager finishes its one-frame load/refresh pass.")]
    public bool waitForGameSaveLoad = true;

    [Tooltip("Small delay to let SpawnManager, save/load, and scene registries finish refreshing.")]
    public float startDelay = 0.05f;

    private Collider2D cachedCollider;
    private bool startPending;
    private bool hasTriggered;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (ShouldBeDisabledBecauseAlreadyActivated())
        {
            DisableOrDestroyActivator();
            return;
        }

        if (startOnSceneStart)
        {
            RequestStartCinematic();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        RequestStartCinematic();
    }

    public void RefreshActivationState()
    {
        if (ShouldBeDisabledBecauseAlreadyActivated())
        {
            DisableOrDestroyActivator();
        }
    }

    private void RequestStartCinematic()
    {
        if (hasTriggered || startPending)
        {
            return;
        }

        if (sceneData == null)
        {
            Debug.LogWarning($"{name} has no SceneData assigned.");
            return;
        }

        StartCoroutine(StartCinematicWhenReady());
    }

    private IEnumerator StartCinematicWhenReady()
    {
        startPending = true;
        hasTriggered = true;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (FilmSceneManager.Instance == null)
        {
            yield return null;
        }

        while (waitForGameSaveLoad && GameSaveManager.Instance != null && !GameSaveManager.Instance.HasLoaded)
        {
            yield return null;
        }

        while (sceneData != null &&
               FilmSceneManager.Instance != null &&
               !FilmSceneManager.Instance.CanStartWorldCinematicNow(sceneData, false))
        {
            if (FilmSceneManager.Instance.IsCinematicActivated(sceneData))
            {
                DisableOrDestroyActivator();
                yield break;
            }

            yield return null;
        }

        // One extra frame prevents same-frame save/load refreshes from fighting the cinematic start.
        yield return null;

        if (sceneData == null || FilmSceneManager.Instance == null)
        {
            startPending = false;
            hasTriggered = false;
            yield break;
        }

        if (FilmSceneManager.Instance.IsCinematicActivated(sceneData))
        {
            DisableOrDestroyActivator();
            yield break;
        }

        bool started = FilmSceneManager.Instance.TryStartCinematic(sceneData);

        startPending = false;

        if (!started)
        {
            hasTriggered = false;
            yield break;
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
        StopAllCoroutines();
        startPending = false;
        hasTriggered = true;

        if (destroyAfterUse)
        {
            Destroy(gameObject);
            return;
        }

        if (cachedCollider == null)
        {
            cachedCollider = GetComponent<Collider2D>();
        }

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        enabled = false;
    }
}
