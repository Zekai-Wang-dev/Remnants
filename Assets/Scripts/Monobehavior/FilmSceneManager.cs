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

    [Header("Queued Cinematics")]
    [SerializeField] private bool queueCompletionCinematicsUntilWorldReady = true;
    [SerializeField] private bool blockQueuedCinematicsInCombatScene = true;
    [SerializeField] private bool blockQueuedCinematicsDuringSpawnTransitions = true;
    [SerializeField] private bool requireSceneCharacterRegistryForQueuedCinematics = true;

    [Header("Persistent Cinematic State")]
    [Tooltip("Assign permanent SceneData assets here when they can be triggered without a CinematicsActivator, such as quest-completion cinematics. This lets saved activated cinematic IDs re-apply Destroy/Disable end commands after scene load.")]
    [SerializeField] private List<SceneData> registeredPermanentCinematics = new List<SceneData>();

    private Coroutine activeCinematicCoroutine;
    private Coroutine delayedRefreshCoroutine;
    private Coroutine queuedCinematicCoroutine;
    private bool isRefreshingWorldState;

    public bool IsRefreshingWorldState => isRefreshingWorldState;

    public bool IsReadyToStartCinematic
    {
        get
        {
            bool saveLoaded = GameSaveManager.Instance == null || GameSaveManager.Instance.HasLoaded;
            return saveLoaded && !isRefreshingWorldState;
        }
    }

    private readonly HashSet<string> activatedSceneIds = new HashSet<string>();
    private readonly Queue<SceneData> queuedCinematics = new Queue<SceneData>();
    private readonly HashSet<string> queuedCinematicIds = new HashSet<string>();
    private readonly HashSet<SceneData> knownSceneDataReferences = new HashSet<SceneData>();

    private SceneData pendingSceneEndCombatCinematic;
    private string pendingSceneEndCombatCinematicId = "";

    private SceneData pendingSceneEndCombatVictoryCinematic;
    private string pendingSceneEndCombatVictoryCinematicId = "";
    private Coroutine pendingSceneEndCombatVictoryCoroutine;

    public bool HasPendingSceneEndCombatResolution => pendingSceneEndCombatVictoryCinematic != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterInspectorPermanentCinematics();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        CancelActiveCinematic("FilmSceneManager destroyed", true);
        Instance = null;
    }

    public void RequestDelayedCinematicWorldRefresh()
    {
        if (delayedRefreshCoroutine != null)
        {
            StopCoroutine(delayedRefreshCoroutine);
        }

        delayedRefreshCoroutine = StartCoroutine(DelayedCinematicWorldRefresh());
    }

    private IEnumerator DelayedCinematicWorldRefresh()
    {
        yield return null;
        yield return null;

        delayedRefreshCoroutine = null;
        RefreshCinematicWorldState();
    }

    private void RegisterInspectorPermanentCinematics()
    {
        if (registeredPermanentCinematics == null)
        {
            return;
        }

        foreach (SceneData sceneData in registeredPermanentCinematics)
        {
            RegisterKnownCinematicSceneData(sceneData);
        }
    }

    public void RegisterKnownCinematicSceneData(SceneData sceneData)
    {
        if (sceneData == null)
        {
            return;
        }

        knownSceneDataReferences.Add(sceneData);
    }

    public void QueueCinematic(SceneData filmScene)
    {
        if (filmScene == null)
        {
            Debug.LogWarning("Tried to queue null cinematic.");
            return;
        }

        RegisterKnownCinematicSceneData(filmScene);

        string cinematicId = GetCinematicId(filmScene);

        if (IsCinematicActivated(filmScene))
        {
            PurgeQueuedCinematic(filmScene);
            Debug.Log($"Queued cinematic '{cinematicId}' was skipped because it has already been activated.");
            return;
        }

        if (IsCinematicIdPendingSceneEndCombat(cinematicId))
        {
            Debug.Log($"Cinematic '{cinematicId}' was not queued because it is already waiting for a scene-end combat result.");
            return;
        }

        if (!queueCompletionCinematicsUntilWorldReady)
        {
            TryStartCinematic(filmScene);
            return;
        }

        if (IsCinematicAlreadyQueued(filmScene, cinematicId))
        {
            Debug.Log($"Cinematic '{cinematicId}' is already queued.");
            return;
        }

        queuedCinematics.Enqueue(filmScene);

        if (!string.IsNullOrWhiteSpace(cinematicId))
        {
            queuedCinematicIds.Add(cinematicId);
        }

        EnsureQueuedCinematicCoroutineRunning();
    }

    private void EnsureQueuedCinematicCoroutineRunning()
    {
        if (queuedCinematicCoroutine != null)
        {
            return;
        }

        queuedCinematicCoroutine = StartCoroutine(PlayQueuedCinematicsWhenReady());
    }

    private IEnumerator PlayQueuedCinematicsWhenReady()
    {
        while (queuedCinematics.Count > 0)
        {
            SceneData nextCinematic = queuedCinematics.Peek();
            string cinematicId = GetCinematicId(nextCinematic);

            yield return new WaitUntil(CanPlayQueuedCinematic);

            if (nextCinematic == null)
            {
                RemovePeekedQueuedCinematic(cinematicId);
                continue;
            }

            if (IsCinematicActivated(nextCinematic))
            {
                RemovePeekedQueuedCinematic(cinematicId);
                continue;
            }

            if (IsCinematicIdPendingSceneEndCombat(cinematicId))
            {
                RemovePeekedQueuedCinematic(cinematicId);
                continue;
            }

            bool started = TryStartCinematic(nextCinematic);

            if (!started)
            {
                // Do not drop the cinematic. This can happen during a same-frame
                // combat return / scene refresh race. Keep it queued and try again.
                Debug.Log($"Queued cinematic '{cinematicId}' is still waiting for a safe world state.");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            RemovePeekedQueuedCinematic(cinematicId);

            yield return new WaitUntil(() => !inCinematic && activeCinematicCoroutine == null);
        }

        queuedCinematicCoroutine = null;
    }



    private void RemovePeekedQueuedCinematic(string cinematicId)
    {
        if (queuedCinematics.Count > 0)
        {
            queuedCinematics.Dequeue();
        }

        if (!string.IsNullOrWhiteSpace(cinematicId))
        {
            queuedCinematicIds.Remove(cinematicId);
        }
    }

    private bool IsCinematicAlreadyQueued(SceneData filmScene, string cinematicId)
    {
        if (!string.IsNullOrWhiteSpace(cinematicId) && queuedCinematicIds.Contains(cinematicId))
        {
            return true;
        }

        foreach (SceneData queuedCinematic in queuedCinematics)
        {
            if (queuedCinematic == filmScene)
            {
                return true;
            }
        }

        return false;
    }

    private void PurgeQueuedCinematic(SceneData filmScene)
    {
        if (filmScene == null || queuedCinematics.Count == 0)
        {
            return;
        }

        string cinematicId = GetCinematicId(filmScene);
        Queue<SceneData> keptCinematics = new Queue<SceneData>();
        queuedCinematicIds.Clear();

        while (queuedCinematics.Count > 0)
        {
            SceneData queuedCinematic = queuedCinematics.Dequeue();
            string queuedId = GetCinematicId(queuedCinematic);

            bool sameReference = queuedCinematic == filmScene;
            bool sameId = !string.IsNullOrWhiteSpace(cinematicId) && cinematicId == queuedId;

            if (sameReference || sameId)
            {
                continue;
            }

            keptCinematics.Enqueue(queuedCinematic);

            if (!string.IsNullOrWhiteSpace(queuedId))
            {
                queuedCinematicIds.Add(queuedId);
            }
        }

        while (keptCinematics.Count > 0)
        {
            queuedCinematics.Enqueue(keptCinematics.Dequeue());
        }
    }

    private bool IsCinematicIdPendingSceneEndCombat(string cinematicId)
    {
        if (string.IsNullOrWhiteSpace(cinematicId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(pendingSceneEndCombatCinematicId) &&
            cinematicId == pendingSceneEndCombatCinematicId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(pendingSceneEndCombatVictoryCinematicId) &&
            cinematicId == pendingSceneEndCombatVictoryCinematicId)
        {
            return true;
        }

        return false;
    }

    private bool IsCombatBlockingCinematics()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive)
        {
            return true;
        }

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager != null && spawnManager.IsCurrentSceneCombat)
        {
            return true;
        }

        return false;
    }

    public bool IsWorldReadyForCinematic(SceneData filmScene = null, bool logWarnings = false)
    {
        if (!IsReadyToStartCinematic)
        {
            if (logWarnings)
            {
                Debug.LogWarning("Cannot start cinematic because save/load or cinematic world refresh is not finished.");
            }

            return false;
        }

        if (inCinematic || activeCinematicCoroutine != null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("Cannot start cinematic because another cinematic is already active.");
            }

            return false;
        }

        if (IsCombatBlockingCinematics())
        {
            if (logWarnings)
            {
                Debug.LogWarning("Cannot start cinematic while combat is active, ending, or returning to the overworld.");
            }

            return false;
        }

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager != null && spawnManager.IsSceneTransitioning)
        {
            if (logWarnings)
            {
                Debug.LogWarning("Cannot start cinematic during a SpawnManager scene transition.");
            }

            return false;
        }

        if (requireSceneCharacterRegistryForQueuedCinematics && SceneCharacterRegistry.Instance == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("Cannot start cinematic because SceneCharacterRegistry is missing.");
            }

            return false;
        }

        if (filmScene != null && IsCinematicActivated(filmScene))
        {
            if (logWarnings)
            {
                Debug.Log($"Cinematic '{GetCinematicId(filmScene)}' has already been activated.");
            }

            return false;
        }

        return true;
    }

    private bool CanPlayQueuedCinematic()
    {
        if (pendingSceneEndCombatVictoryCinematic != null ||
            pendingSceneEndCombatVictoryCoroutine != null)
        {
            return false;
        }

        return IsWorldReadyForCinematic(null, false);
    }

    public bool CanStartWorldCinematicNow(SceneData filmScene, bool logWarnings = false)
    {
        if (filmScene == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("Tried to check a null cinematic.");
            }

            return false;
        }

        if (pendingSceneEndCombatVictoryCinematic != null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("Cannot start cinematic because scene-end combat victory cleanup is still waiting to resolve.");
            }

            return false;
        }

        return IsWorldReadyForCinematic(filmScene, logWarnings);
    }

    public void StartCinematic(SceneData filmScene)
    {
        TryStartCinematic(filmScene);
    }

    public bool TryStartCinematic(SceneData filmScene)
    {
        if (filmScene == null)
        {
            Debug.LogWarning("Tried to start null cinematic.");
            return false;
        }

        RegisterKnownCinematicSceneData(filmScene);

        string cinematicId = GetCinematicId(filmScene);

        if (IsCinematicIdPendingSceneEndCombat(cinematicId))
        {
            Debug.Log($"Cinematic '{cinematicId}' is already waiting for a scene-end combat result, so it will not start again.");
            return false;
        }

        if (!CanStartWorldCinematicNow(filmScene, true))
        {
            return false;
        }

        activeCinematicCoroutine = StartCoroutine(PlayCinematic(filmScene));
        return true;
    }

    private IEnumerator PlayCinematic(SceneData filmScene)
    {
        bool cleanupNeeded = true;
        bool startedCombat = false;
        string cinematicId = GetCinematicId(filmScene);

        inCinematic = true;
        currentFilmSceneId = cinematicId;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        try
        {
            if (filmScene.actions != null)
            {
                foreach (SceneAction action in filmScene.actions)
                {
                    yield return StartCoroutine(PlayAction(action));
                }
            }

            startedCombat = ApplyEndCommands(filmScene);

            if (!startedCombat)
            {
                MarkCinematicActivated(filmScene);

                if (filmScene.newQuest != null)
                {
                    QuestDataManager.Instance?.AddQuest(filmScene.newQuest);
                }
            }
            else
            {
                Debug.Log($"Cinematic '{cinematicId}' started scene-end combat. Permanent completion is waiting for combat victory.");
            }

            cleanupNeeded = false;
            FinishCinematic(startedCombat, $"CinematicFinished:{cinematicId}", true);
        }
        finally
        {
            if (cleanupNeeded)
            {
                FinishCinematic(false, $"CinematicInterrupted:{cinematicId}", true);
            }
        }
    }

    public void CancelActiveCinematic(string reason = "Cancelled", bool enablePlayerMovement = true)
    {
        if (!inCinematic && activeCinematicCoroutine == null)
        {
            return;
        }

        StopAllCoroutines();
        activeCinematicCoroutine = null;
        delayedRefreshCoroutine = null;
        queuedCinematicCoroutine = null;
        pendingSceneEndCombatVictoryCoroutine = null;

        DestroyActiveSpeechBubbles();
        FinishCinematic(false, reason, enablePlayerMovement);

        if (pendingSceneEndCombatVictoryCinematic != null)
        {
            EnsureSceneEndCombatVictoryCoroutineRunning();
        }

        if (queuedCinematics.Count > 0)
        {
            EnsureQueuedCinematicCoroutineRunning();
        }
    }

    private void FinishCinematic(bool startedCombat, string reason, bool enablePlayerMovement)
    {
        activeCinematicCoroutine = null;
        inCinematic = false;
        currentFilmSceneId = "";

        if (!enablePlayerMovement || startedCombat)
        {
            return;
        }

        if (PlayerController.Instance == null)
        {
            return;
        }

        if (PlayerController.Instance.MovementDisabledByCombat)
        {
            return;
        }

        PlayerController.Instance.ForceEnableMovement(reason);
    }

    private void DestroyActiveSpeechBubbles()
    {
        SpeechBubbleUI[] bubbles = FindObjectsByType<SpeechBubbleUI>(FindObjectsSortMode.None);

        foreach (SpeechBubbleUI bubble in bubbles)
        {
            if (bubble != null)
            {
                Destroy(bubble.gameObject);
            }
        }
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

            case SceneAction.ActionType.TurnAround:
                yield return StartCoroutine(PlayTurnAroundAction(action));
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

    private IEnumerator PlayTurnAroundAction(SceneAction action)
    {
        if (action == null)
        {
            yield break;
        }

        if (action.actorCommands == null || action.actorCommands.Count == 0)
        {
            Debug.LogWarning($"TurnAround action '{action.actionName}' has no actor commands.");
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

            SpriteRenderer spriteRenderer = actor.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = actor.GetComponentInChildren<SpriteRenderer>(true);
            }

            if (spriteRenderer == null)
            {
                Debug.LogWarning($"Actor '{command.actorId}' has no SpriteRenderer to flip.");
                continue;
            }

            switch (command.spriteFlipMode)
            {
                case SceneActorCommand.SpriteFlipMode.Toggle:
                    spriteRenderer.flipX = !spriteRenderer.flipX;
                    break;

                case SceneActorCommand.SpriteFlipMode.SetValue:
                    spriteRenderer.flipX = command.targetFlipX;
                    break;
            }
        }
    }

    private IEnumerator PlayDialogueAction(SceneAction action)
    {
        if (action == null)
        {
            yield break;
        }

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

    private Transform GetActor(string actorId, bool logWarnings = true)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return null;
        }

        if (SceneCharacterRegistry.Instance == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("SceneCharacterRegistry is missing, so the cinematic actor lookup cannot be resolved.");
            }

            return null;
        }

        return SceneCharacterRegistry.Instance.GetActor(actorId, logWarnings);
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

    private bool ApplyEndCommands(SceneData filmScene)
    {
        if (filmScene == null || filmScene.endCommands == null)
        {
            return false;
        }

        // If this cinematic ends in combat, treat the combat result as the real
        // completion condition. This prevents permanent cleanup commands from
        // deleting actors/triggers before the player has actually won.
        if (TryStartAnyCombatEndCommand(filmScene))
        {
            return true;
        }

        foreach (SceneEndActorCommand command in filmScene.endCommands)
        {
            if (command == null)
            {
                continue;
            }

            switch (command.endBehavior)
            {
                case SceneEndActorCommand.EndBehavior.None:
                    break;

                case SceneEndActorCommand.EndBehavior.Disable:
                    ApplyActorEndCommand(command, disable: true, warnIfMissing: true);
                    break;

                case SceneEndActorCommand.EndBehavior.Destroy:
                    ApplyActorEndCommand(command, disable: false, warnIfMissing: true);
                    break;

                case SceneEndActorCommand.EndBehavior.Quest:
                    if (filmScene.newQuest != null)
                    {
                        QuestDataManager.Instance?.AddQuest(filmScene.newQuest);
                    }
                    break;

                case SceneEndActorCommand.EndBehavior.Combat:
                    break;
            }
        }

        return false;
    }

    private bool TryStartAnyCombatEndCommand(SceneData filmScene)
    {
        if (filmScene == null || filmScene.endCommands == null)
        {
            return false;
        }

        foreach (SceneEndActorCommand command in filmScene.endCommands)
        {
            if (command == null || command.endBehavior != SceneEndActorCommand.EndBehavior.Combat)
            {
                continue;
            }

            if (StartCombatEndCommand(filmScene, command))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyActorEndCommand(SceneEndActorCommand command, bool disable, bool warnIfMissing)
    {
        if (command == null)
        {
            return;
        }

        Transform actor = GetActor(command.actorId, warnIfMissing);

        if (actor == null)
        {
            if (warnIfMissing)
            {
                Debug.LogWarning($"End command could not find actor '{command.actorId}'.");
            }

            return;
        }

        if (disable)
        {
            actor.gameObject.SetActive(false);
        }
        else
        {
            Destroy(actor.gameObject);
        }
    }

    private bool StartCombatEndCommand(SceneData filmScene, SceneEndActorCommand command)
    {
        if (command == null || command.combatInteraction == null)
        {
            Debug.LogWarning("Combat end command has no CombatInteraction data.");
            return false;
        }

        if (command.combatInteraction.enemies == null || command.combatInteraction.enemies.Count == 0)
        {
            Debug.LogWarning($"Combat end command on cinematic '{GetCinematicId(filmScene)}' has no enemies assigned.");
            return false;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogWarning("Cannot start scene-end combat because SpawnManager is missing.");
            return false;
        }

        string combatId = !string.IsNullOrWhiteSpace(command.combatInteraction.combatId)
            ? command.combatInteraction.combatId.Trim()
            : BuildSceneEndCombatId(filmScene, command);

        bool started = SpawnManager.Instance.StartCombatFromEnemyPool(
            command.combatInteraction.enemies,
            combatId
        );

        if (started)
        {
            pendingSceneEndCombatCinematic = filmScene;
            pendingSceneEndCombatCinematicId = GetCinematicId(filmScene);
            RegisterKnownCinematicSceneData(filmScene);

            Debug.Log($"Cinematic end command started combat: {combatId}. Cinematic completion will resolve after combat result.");
        }

        return started;
    }

    public void ResolveSceneEndCombatResult(bool playerWon)
    {
        if (pendingSceneEndCombatCinematic == null)
        {
            return;
        }

        SceneData completedCinematic = pendingSceneEndCombatCinematic;
        string completedCinematicId = pendingSceneEndCombatCinematicId;

        pendingSceneEndCombatCinematic = null;
        pendingSceneEndCombatCinematicId = "";

        // If this same cinematic was accidentally queued during combat, remove it.
        // The combat result is now the authority for whether this cinematic completed.
        PurgeQueuedCinematic(completedCinematic);

        if (!playerWon)
        {
            Debug.Log($"Scene-end combat for cinematic '{completedCinematicId}' was lost. Cinematic will not be marked activated, so its trigger/actors can remain for retry.");
            return;
        }

        Debug.Log($"Scene-end combat for cinematic '{completedCinematicId}' was won. Waiting until the overworld is loaded before applying cinematic completion.");

        pendingSceneEndCombatVictoryCinematic = completedCinematic;
        pendingSceneEndCombatVictoryCinematicId = completedCinematicId;
        RegisterKnownCinematicSceneData(completedCinematic);

        EnsureSceneEndCombatVictoryCoroutineRunning();
    }

    private void EnsureSceneEndCombatVictoryCoroutineRunning()
    {
        if (pendingSceneEndCombatVictoryCoroutine != null)
        {
            return;
        }

        pendingSceneEndCombatVictoryCoroutine = StartCoroutine(ResolveSceneEndCombatVictoryWhenWorldReady());
    }

    private IEnumerator ResolveSceneEndCombatVictoryWhenWorldReady()
    {
        while (pendingSceneEndCombatVictoryCinematic != null)
        {
            yield return new WaitUntil(CanResolveSceneEndCombatVictory);

            SceneData completedCinematic = pendingSceneEndCombatVictoryCinematic;
            string completedCinematicId = pendingSceneEndCombatVictoryCinematicId;

            pendingSceneEndCombatVictoryCinematic = null;
            pendingSceneEndCombatVictoryCinematicId = "";

            Debug.Log($"Applying post-combat cinematic completion for '{completedCinematicId}' after returning to the overworld.");

            ApplyPostCombatVictoryEndCommands(completedCinematic);
            MarkCinematicActivated(completedCinematic);

            if (completedCinematic != null && completedCinematic.newQuest != null)
            {
                QuestDataManager.Instance?.AddQuest(completedCinematic.newQuest);
            }

            yield return null;
        }

        pendingSceneEndCombatVictoryCoroutine = null;

        if (queuedCinematics.Count > 0)
        {
            EnsureQueuedCinematicCoroutineRunning();
        }
    }

    private bool CanResolveSceneEndCombatVictory()
    {
        if (!IsReadyToStartCinematic)
        {
            return false;
        }

        if (inCinematic || activeCinematicCoroutine != null)
        {
            return false;
        }

        if (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive)
        {
            return false;
        }

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager != null)
        {
            if (spawnManager.IsSceneTransitioning)
            {
                return false;
            }

            if (spawnManager.IsCurrentSceneCombat)
            {
                return false;
            }
        }

        if (requireSceneCharacterRegistryForQueuedCinematics && SceneCharacterRegistry.Instance == null)
        {
            return false;
        }

        return true;
    }

    private void ApplyPostCombatVictoryEndCommands(SceneData filmScene)
    {
        if (filmScene == null || filmScene.endCommands == null)
        {
            return;
        }

        foreach (SceneEndActorCommand command in filmScene.endCommands)
        {
            if (command == null)
            {
                continue;
            }

            switch (command.endBehavior)
            {
                case SceneEndActorCommand.EndBehavior.Disable:
                    ApplyActorEndCommand(command, disable: true, warnIfMissing: false);
                    break;

                case SceneEndActorCommand.EndBehavior.Destroy:
                    ApplyActorEndCommand(command, disable: false, warnIfMissing: false);
                    break;

                case SceneEndActorCommand.EndBehavior.Quest:
                    if (filmScene.newQuest != null)
                    {
                        QuestDataManager.Instance?.AddQuest(filmScene.newQuest);
                    }
                    break;

                case SceneEndActorCommand.EndBehavior.None:
                case SceneEndActorCommand.EndBehavior.Combat:
                    break;
            }
        }
    }

    private string BuildSceneEndCombatId(SceneData filmScene, SceneEndActorCommand command)
    {
        string sceneId = filmScene != null ? GetCinematicId(filmScene) : "unknown_scene";
        string actorId = command != null && !string.IsNullOrWhiteSpace(command.actorId)
            ? command.actorId.Trim()
            : "no_actor";

        return $"cinematic_{sceneId}_{actorId}";
    }

    public bool IsCinematicActivated(SceneData filmScene)
    {
        if (filmScene == null)
        {
            return false;
        }

        if (!filmScene.isPermanent)
        {
            return false;
        }

        string cinematicId = GetCinematicId(filmScene);

        if (string.IsNullOrWhiteSpace(cinematicId))
        {
            return false;
        }

        return activatedSceneIds.Contains(cinematicId);
    }

    private void MarkCinematicActivated(SceneData filmScene)
    {
        if (filmScene == null)
        {
            return;
        }

        RegisterKnownCinematicSceneData(filmScene);

        if (!filmScene.isPermanent)
        {
            return;
        }

        string cinematicId = GetCinematicId(filmScene);

        if (string.IsNullOrWhiteSpace(cinematicId))
        {
            Debug.LogWarning($"Permanent cinematic '{filmScene.name}' has no sceneId.");
            return;
        }

        PurgeQueuedCinematic(filmScene);

        filmScene.isActivated = true;

        if (activatedSceneIds.Add(cinematicId))
        {
            GameSaveManager.Instance?.RequestSave();
            RefreshCinematicWorldState();

            Debug.Log($"Permanent cinematic activated: {cinematicId}");
        }
    }

    public CinematicSaveData CreateSaveData()
    {
        CinematicSaveData saveData = new CinematicSaveData();

        foreach (string sceneId in activatedSceneIds)
        {
            if (!string.IsNullOrWhiteSpace(sceneId))
            {
                saveData.activatedSceneIds.Add(sceneId);
            }
        }

        return saveData;
    }

    public void LoadFromSaveData(CinematicSaveData saveData)
    {
        CancelActiveCinematic("CinematicSaveDataLoaded", true);

        activatedSceneIds.Clear();
        queuedCinematics.Clear();
        queuedCinematicIds.Clear();
        pendingSceneEndCombatCinematic = null;
        pendingSceneEndCombatCinematicId = "";
        pendingSceneEndCombatVictoryCinematic = null;
        pendingSceneEndCombatVictoryCinematicId = "";
        pendingSceneEndCombatVictoryCoroutine = null;

        if (saveData != null && saveData.activatedSceneIds != null)
        {
            foreach (string sceneId in saveData.activatedSceneIds)
            {
                string normalizedId = NormalizeCinematicId(sceneId);

                if (!string.IsNullOrWhiteSpace(normalizedId))
                {
                    activatedSceneIds.Add(normalizedId);
                }
            }
        }

        RefreshCinematicWorldState();

        Debug.Log($"Loaded {activatedSceneIds.Count} activated cinematics.");
    }

    public void ClearRuntimeCinematicData()
    {
        CancelActiveCinematic("CinematicRuntimeDataCleared", true);

        activatedSceneIds.Clear();
        queuedCinematics.Clear();
        queuedCinematicIds.Clear();
        pendingSceneEndCombatCinematic = null;
        pendingSceneEndCombatCinematicId = "";
        pendingSceneEndCombatVictoryCinematic = null;
        pendingSceneEndCombatVictoryCinematicId = "";
        pendingSceneEndCombatVictoryCoroutine = null;
        currentFilmSceneId = "";
        inCinematic = false;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ForceEnableMovement("CinematicRuntimeDataCleared");
        }

        RefreshCinematicWorldState();

        Debug.Log("Cinematic runtime data cleared.");
    }

    public void RefreshCinematicWorldState()
    {
        isRefreshingWorldState = true;

        try
        {
            if (SceneCharacterRegistry.Instance != null)
            {
                SceneCharacterRegistry.Instance.SendMessage(
                    "RefreshAfterCinematicStateChanged",
                    SendMessageOptions.DontRequireReceiver
                );
            }

            ReapplyCompletedCinematicActorEndCommands();
            RefreshCinematicActivators();
            RefreshPersistentCinematicObjects();
        }
        finally
        {
            isRefreshingWorldState = false;
        }
    }

    private void RefreshCinematicActivators()
    {
        CinematicsActivator[] activators = FindObjectsByType<CinematicsActivator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CinematicsActivator activator in activators)
        {
            if (activator == null)
            {
                continue;
            }

            activator.RefreshActivationState();
        }
    }

    private void RefreshPersistentCinematicObjects()
    {
        PersistentCinematicObject[] cinematicObjects = FindObjectsByType<PersistentCinematicObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (PersistentCinematicObject cinematicObject in cinematicObjects)
        {
            if (cinematicObject == null)
            {
                continue;
            }

            cinematicObject.RefreshCinematicObjectState();
        }
    }

    private void ReapplyCompletedCinematicActorEndCommands()
    {
        HashSet<SceneData> sceneDatas = GetLoadedSceneDataReferences();

        foreach (SceneData sceneData in sceneDatas)
        {
            if (sceneData == null)
            {
                continue;
            }

            if (!IsCinematicActivated(sceneData))
            {
                continue;
            }

            ReapplyPersistentActorEndCommands(sceneData);
        }
    }

    private HashSet<SceneData> GetLoadedSceneDataReferences()
    {
        HashSet<SceneData> sceneDatas = new HashSet<SceneData>();

        RegisterInspectorPermanentCinematics();

        foreach (SceneData sceneData in knownSceneDataReferences)
        {
            if (sceneData != null)
            {
                sceneDatas.Add(sceneData);
            }
        }

        if (registeredPermanentCinematics != null)
        {
            foreach (SceneData sceneData in registeredPermanentCinematics)
            {
                if (sceneData != null)
                {
                    sceneDatas.Add(sceneData);
                }
            }
        }

        CinematicsActivator[] activators = FindObjectsByType<CinematicsActivator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CinematicsActivator activator in activators)
        {
            if (activator == null || activator.sceneData == null)
            {
                continue;
            }

            sceneDatas.Add(activator.sceneData);
        }

        return sceneDatas;
    }

    private void ReapplyPersistentActorEndCommands(SceneData sceneData)
    {
        if (sceneData == null || sceneData.endCommands == null)
        {
            return;
        }

        foreach (SceneEndActorCommand command in sceneData.endCommands)
        {
            if (command == null)
            {
                continue;
            }

            switch (command.endBehavior)
            {
                case SceneEndActorCommand.EndBehavior.Disable:
                    ApplyActorEndCommand(command, disable: true, warnIfMissing: false);
                    break;

                case SceneEndActorCommand.EndBehavior.Destroy:
                    ApplyActorEndCommand(command, disable: false, warnIfMissing: false);
                    break;
            }
        }
    }

    private string GetCinematicId(SceneData filmScene)
    {
        if (filmScene == null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(filmScene.sceneId))
        {
            return NormalizeCinematicId(filmScene.sceneId);
        }

        return NormalizeCinematicId(filmScene.name);
    }

    private string NormalizeCinematicId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return value.Trim().ToLowerInvariant();
    }
}