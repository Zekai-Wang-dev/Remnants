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

        if (!queueCompletionCinematicsUntilWorldReady)
        {
            TryStartCinematic(filmScene);
            return;
        }

        if (IsCinematicActivated(filmScene))
        {
            Debug.Log($"Queued cinematic '{GetCinematicId(filmScene)}' was skipped because it has already been activated.");
            return;
        }

        string cinematicId = GetCinematicId(filmScene);

        if (!string.IsNullOrWhiteSpace(cinematicId) && queuedCinematicIds.Contains(cinematicId))
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

            queuedCinematics.Dequeue();

            if (!string.IsNullOrWhiteSpace(cinematicId))
            {
                queuedCinematicIds.Remove(cinematicId);
            }

            if (nextCinematic == null)
            {
                continue;
            }

            if (IsCinematicActivated(nextCinematic))
            {
                continue;
            }

            bool started = TryStartCinematic(nextCinematic);

            if (!started)
            {
                Debug.LogWarning($"Queued cinematic '{cinematicId}' could not start even though the world appeared ready.");
                yield return null;
                continue;
            }

            yield return new WaitUntil(() => !inCinematic && activeCinematicCoroutine == null);
        }

        queuedCinematicCoroutine = null;
    }

    private bool CanPlayQueuedCinematic()
    {
        if (!IsReadyToStartCinematic)
        {
            return false;
        }

        if (inCinematic || activeCinematicCoroutine != null)
        {
            return false;
        }

        if (PlayerController.Instance == null)
        {
            return false;
        }

        if (requireSceneCharacterRegistryForQueuedCinematics && SceneCharacterRegistry.Instance == null)
        {
            return false;
        }

        SpawnManager spawnManager = SpawnManager.Instance;

        if (spawnManager != null)
        {
            if (blockQueuedCinematicsDuringSpawnTransitions && spawnManager.IsSceneTransitioning)
            {
                return false;
            }

            if (blockQueuedCinematicsInCombatScene && spawnManager.IsCurrentSceneCombat)
            {
                return false;
            }
        }

        return true;
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

        if (!IsReadyToStartCinematic)
        {
            Debug.LogWarning($"Cinematic '{GetCinematicId(filmScene)}' tried to start before save/load world refresh finished.");
            return false;
        }

        if (inCinematic || activeCinematicCoroutine != null)
        {
            Debug.LogWarning("Already in cinematic.");
            return false;
        }

        if (IsCinematicActivated(filmScene))
        {
            Debug.Log($"Cinematic '{GetCinematicId(filmScene)}' has already been activated.");
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
            MarkCinematicActivated(filmScene);

            if (filmScene.newQuest != null)
            {
                QuestDataManager.Instance?.AddQuest(filmScene.newQuest);
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

        DestroyActiveSpeechBubbles();
        FinishCinematic(false, reason, enablePlayerMovement);

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

    private bool ApplyEndCommands(SceneData filmScene)
    {
        if (filmScene == null || filmScene.endCommands == null)
        {
            return false;
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
                    if (StartCombatEndCommand(filmScene, command))
                    {
                        return true;
                    }
                    break;
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

        Transform actor = GetActor(command.actorId);

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
            Debug.Log($"Cinematic end command started combat: {combatId}");
        }

        return started;
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