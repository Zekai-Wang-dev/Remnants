using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilmSceneManager : MonoBehaviour
{
    public static FilmSceneManager Instance { get; private set; }

    public bool inCinematic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartCinematic(SceneData filmScene)
    {
        if (filmScene == null)
        {
            Debug.LogWarning("Tried to start null cinematic.");
            return;
        }

        if (inCinematic)
        {
            Debug.LogWarning("Already in cinematic.");
            return;
        }

        StartCoroutine(PlayCinematic(filmScene));
    }

    private IEnumerator PlayCinematic(SceneData filmScene)
    {
        inCinematic = true;

        foreach (SceneAction action in filmScene.actions)
        {
            yield return StartCoroutine(PlayAction(action));
        }

        ApplyEndCommands(filmScene);

        inCinematic = false;
    }

    private IEnumerator PlayAction(SceneAction action)
    {
        if (action == null) yield break;

        Debug.Log("Playing film action: " + action.actionName);

        switch (action.type)
        {
            case SceneAction.ActionType.Move:
                yield return StartCoroutine(PlayMoveAction(action));
                break;

            case SceneAction.ActionType.Animation:
                PlayAnimationAction(action);
                break;

            case SceneAction.ActionType.Dialogue:
                // Add dialogue logic later.
                Debug.Log("Dialogue action not implemented yet.");
                break;

            case SceneAction.ActionType.Wait:
                yield return new WaitForSeconds(action.waitAfterAction);
                yield break;
        }

        if (action.waitAfterAction > 0f)
        {
            yield return new WaitForSeconds(action.waitAfterAction);
        }
    }

    private IEnumerator PlayMoveAction(SceneAction action)
    {
        if (action.actorCommands == null || action.actorCommands.Count == 0)
        {
            Debug.LogWarning($"Move action '{action.actionName}' has no actor commands.");
            yield break;
        }

        List<Coroutine> runningMoves = new();

        foreach (SceneActorCommand command in action.actorCommands)
        {
            Transform actor = SceneCharacterRegistry.Instance.GetActor(command.actorId);

            if (actor == null)
            {
                Debug.LogWarning($"Could not find actor with ID '{command.actorId}'.");
                continue;
            }

            Coroutine moveRoutine = StartCoroutine(MoveActor(actor, command));
            runningMoves.Add(moveRoutine);
        }

        // Wait until the longest movement should be finished.
        float longestDuration = GetLongestMoveDuration(action.actorCommands);
        yield return new WaitForSeconds(longestDuration);
    }

    private IEnumerator MoveActor(Transform actor, SceneActorCommand command)
    {
        Vector3 startPosition = actor.position;

        Vector3 direction = new Vector3(
            command.moveDirection.x,
            command.moveDirection.y,
            0f
        ).normalized;

        Vector3 endPosition = startPosition + direction * command.moveDistance;

        float speed = Mathf.Max(0.01f, command.moveSpeed);
        float duration = command.moveDistance / speed;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            actor.position = Vector3.Lerp(startPosition, endPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        actor.position = endPosition;
    }

    private float GetLongestMoveDuration(List<SceneActorCommand> commands)
    {
        float longest = 0f;

        foreach (SceneActorCommand command in commands)
        {
            if (command == null) continue;

            float speed = Mathf.Max(0.01f, command.moveSpeed);
            float duration = command.moveDistance / speed;

            if (duration > longest)
            {
                longest = duration;
            }
        }

        return longest;
    }

    private void PlayAnimationAction(SceneAction action)
    {
        if (action.actorCommands == null || action.actorCommands.Count == 0)
        {
            Debug.LogWarning($"Animation action '{action.actionName}' has no actor commands.");
            return;
        }

        foreach (SceneActorCommand command in action.actorCommands)
        {
            Transform actor = SceneCharacterRegistry.Instance.GetActor(command.actorId);

            if (actor == null)
            {
                Debug.LogWarning($"Could not find actor with ID '{command.actorId}'.");
                continue;
            }

            Animator animator = actor.GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogWarning($"Actor '{command.actorId}' has no Animator.");
                continue;
            }

            if (command.animationClip == null)
            {
                Debug.LogWarning($"No animation clip assigned for actor '{command.actorId}'.");
                continue;
            }

            animator.Play(command.animationClip.name);
        }
    }

    private void ApplyEndCommands(SceneData filmScene)
    {
        if (filmScene.endCommands == null) return;

        foreach (SceneEndActorCommand command in filmScene.endCommands)
        {
            Transform actor = SceneCharacterRegistry.Instance.GetActor(command.actorId);

            if (actor == null)
            {
                Debug.LogWarning($"End command could not find actor '{command.actorId}'.");
                continue;
            }

            switch (command.endBehavior)
            {
                case SceneEndActorCommand.EndBehavior.None:
                    break;

                case SceneEndActorCommand.EndBehavior.Disable:
                    actor.gameObject.SetActive(false);
                    break;

                case SceneEndActorCommand.EndBehavior.Destroy:
                    Destroy(actor.gameObject);
                    break;
            }
        }
    }
}