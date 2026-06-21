using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilmSceneManager : MonoBehaviour
{
    public static FilmSceneManager Instance { get; private set; }

    [Header("Dialogue Defaults")]
    public GameObject defaultSpeechBubblePrefab;
    public float defaultCharactersPerSecond = 30f;
    public KeyCode defaultContinueKey = KeyCode.Space;
    private string currentFilmSceneId;
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
        currentFilmSceneId = filmScene.sceneId;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        foreach (SceneAction action in filmScene.actions)
        {
            yield return StartCoroutine(PlayAction(action));
        }

        ApplyEndCommands(filmScene);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableMovement();
        }

        if (filmScene.newQuest != null)
        {
            QuestDataManager.Instance?.AddQuest(filmScene.newQuest);
        }

        inCinematic = false;
        currentFilmSceneId = "";
    }

    private IEnumerator PlayAction(SceneAction action)
    {
        if (action == null)
        {
            yield break;
        }

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
                yield return StartCoroutine(PlayDialogueAction(action));
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

    private IEnumerator PlayDialogueAction(SceneAction action)
    {
        if (action == null)
        {
            yield break;
        }

        // New preferred workflow: one Dialogue action can contain many inline dialogue beats.
        if (action.dialogueBeats != null && action.dialogueBeats.Count > 0)
        {
            foreach (DialogueBeat beat in action.dialogueBeats)
            {
                if (beat == null)
                {
                    continue;
                }

                yield return StartCoroutine(PlayDialogueBeat(beat));
            }

            yield break;
        }

        // Old workflow is still supported so your existing DialogueLine assets do not instantly break.
        DialogueLine oldLine = GetOldDialogueLine(action);

        if (oldLine == null)
        {
            Debug.LogWarning($"Dialogue action '{action.actionName}' has no dialogueBeats and no old DialogueLine.");
            yield break;
        }

        yield return StartCoroutine(PlayOldDialogueLine(action, oldLine));
    }

    private IEnumerator PlayDialogueBeat(DialogueBeat beat)
    {
        if (beat == null)
        {
            yield break;
        }

        GameObject bubblePrefab = beat.speechBubblePrefab != null
            ? beat.speechBubblePrefab
            : defaultSpeechBubblePrefab;

        if (bubblePrefab == null)
        {
            Debug.LogWarning("Dialogue beat has no speechBubblePrefab and FilmSceneManager has no defaultSpeechBubblePrefab.");
            yield break;
        }

        Transform speaker = GetActor(beat.actorId);
        PlaySpeakingAnimation(speaker, beat);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found for dialogue bubble.");
            yield break;
        }

        GameObject bubbleObject = Instantiate(bubblePrefab, canvas.transform);
        SpeechBubbleUI bubble = bubbleObject.GetComponent<SpeechBubbleUI>();

        if (bubble == null)
        {
            Debug.LogError("Speech bubble prefab needs a SpeechBubbleUI component on its root object.");
            Destroy(bubbleObject);
            yield break;
        }

        float speed = beat.charactersPerSecond > 0f
            ? beat.charactersPerSecond
            : defaultCharactersPerSecond;

        KeyCode continueKey = beat.continueKey != KeyCode.None
            ? beat.continueKey
            : defaultContinueKey;

        yield return StartCoroutine(bubble.Play(
            beat.actorId,
            beat.line,
            speed,
            beat.waitForPlayerInputAfterLine,
            continueKey,
            speaker
        ));

        QuestDataManager.Instance?.ReportTalkToNPC(beat.actorId, currentFilmSceneId);

        PlayAnimationAfterLine(speaker, beat);

        if (beat.waitAfterLine > 0f)
        {
            yield return new WaitForSeconds(beat.waitAfterLine);
        }

    }

    private IEnumerator PlayOldDialogueLine(SceneAction action, DialogueLine line)
    {
        if (line == null)
        {
            yield break;
        }

        GameObject bubblePrefab = line.speechBubblePrefab != null
            ? line.speechBubblePrefab
            : defaultSpeechBubblePrefab;

        if (bubblePrefab == null)
        {
            Debug.LogWarning($"Dialogue line '{line.name}' has no speechBubblePrefab and FilmSceneManager has no defaultSpeechBubblePrefab.");
            yield break;
        }

        Transform speaker = GetOldDialogueSpeaker(action, line);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found for dialogue bubble.");
            yield break;
        }

        GameObject bubbleObject = Instantiate(bubblePrefab, canvas.transform);
        SpeechBubbleUI bubble = bubbleObject.GetComponent<SpeechBubbleUI>();

        if (bubble == null)
        {
            Debug.LogError("Speech bubble prefab needs a SpeechBubbleUI component on its root object.");
            Destroy(bubbleObject);
            yield break;
        }

        yield return StartCoroutine(bubble.Play(line, speaker));
    }

    private DialogueLine GetOldDialogueLine(SceneAction action)
    {
        if (action == null || action.actorCommands == null)
        {
            return null;
        }

        foreach (SceneActorCommand command in action.actorCommands)
        {
            if (command != null && command.dialogueLine != null)
            {
                return command.dialogueLine;
            }
        }

        return null;
    }

    private Transform GetOldDialogueSpeaker(SceneAction action, DialogueLine line)
    {
        string actorId = line != null ? line.characterID : "";

        if (string.IsNullOrWhiteSpace(actorId) && action.actorCommands != null)
        {
            foreach (SceneActorCommand command in action.actorCommands)
            {
                if (command != null && !string.IsNullOrWhiteSpace(command.actorId))
                {
                    actorId = command.actorId;
                    break;
                }
            }
        }

        return GetActor(actorId);
    }

    private Transform GetActor(string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return null;
        }

        if (SceneCharacterRegistry.Instance == null)
        {
            Debug.LogWarning("SceneCharacterRegistry is missing, so dialogue cannot follow an actor.");
            return null;
        }

        Transform actor = SceneCharacterRegistry.Instance.GetActor(actorId);

        if (actor == null)
        {
            Debug.LogWarning($"Could not find actor with ID '{actorId}'.");
        }

        return actor;
    }

    private void PlaySpeakingAnimation(Transform speaker, DialogueBeat beat)
    {
        if (speaker == null || beat == null)
        {
            return;
        }

        Animator animator = speaker.GetComponent<Animator>();
        if (animator == null)
        {
            return;
        }

        if (beat.speakingAnimationClip != null)
        {
            animator.Play(beat.speakingAnimationClip.name);
            return;
        }

        if (!string.IsNullOrWhiteSpace(beat.speakingAnimationStateName))
        {
            animator.Play(beat.speakingAnimationStateName);
        }
    }

    private void PlayAnimationAfterLine(Transform speaker, DialogueBeat beat)
    {
        if (speaker == null || beat == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(beat.animationStateAfterLine))
        {
            return;
        }

        Animator animator = speaker.GetComponent<Animator>();
        if (animator == null)
        {
            return;
        }

        animator.Play(beat.animationStateAfterLine);
    }

    private IEnumerator PlayMoveAction(SceneAction action)
    {
        if (action.actorCommands == null || action.actorCommands.Count == 0)
        {
            Debug.LogWarning($"Move action '{action.actionName}' has no actor commands.");
            yield break;
        }

        foreach (SceneActorCommand command in action.actorCommands)
        {
            if (command == null)
            {
                continue;
            }

            Transform actor = GetActor(command.actorId);

            if (actor == null)
            {
                continue;
            }

            StartCoroutine(MoveActor(actor, command));
        }

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
            if (command == null)
            {
                continue;
            }

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
            if (command == null)
            {
                continue;
            }

            Transform actor = GetActor(command.actorId);

            if (actor == null)
            {
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
        if (filmScene.endCommands == null)
        {
            return;
        }

        foreach (SceneEndActorCommand command in filmScene.endCommands)
        {
            if (command == null)
            {
                continue;
            }

            Transform actor = GetActor(command.actorId);

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

                case SceneEndActorCommand.EndBehavior.Quest:
                    if (filmScene.newQuest != null)
                    {
                        QuestDataManager.Instance?.AddQuest(filmScene.newQuest);
                    }
                    break;
            }
        }
    }
}
