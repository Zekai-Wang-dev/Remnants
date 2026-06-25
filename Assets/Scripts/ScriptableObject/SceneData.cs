using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneData", menuName = "Scriptable Objects/SceneData")]
public class SceneData : ScriptableObject
{
    public string sceneId;
    public string sceneName;

    public List<SceneAction> actions = new();

    public bool isPermanent;
    public bool isActivated;

    [Header("After Scene Ends")]
    public List<SceneEndActorCommand> endCommands = new();
    public OngoingQuestData newQuest;
}

[System.Serializable]
public class SceneAction
{
    public string actionName;
    public ActionType type;

    [Header("Actor Commands")]
    public List<SceneActorCommand> actorCommands = new();

    [Header("Dialogue")]
    [Tooltip("Use this for dialogue actions. Each entry can have its own speaker, text, animation, bubble, typing speed, and continue key.")]
    public List<DialogueBeat> dialogueBeats = new();

    [Header("Timing")]
    public float waitAfterAction = 0.5f;

    public enum ActionType
    {
        None,
        Move,
        TurnAround,
        Animation,
        Dialogue,
        Wait
    }
}

[System.Serializable]
public class DialogueBeat
{
    [Header("Who is speaking")]
    public string actorId;

    [Header("Text")]
    [TextArea(2, 6)]
    public string line;

    [Header("UI")]
    [Tooltip("Optional. If empty, FilmSceneManager.defaultSpeechBubblePrefab will be used.")]
    public GameObject speechBubblePrefab;

    [Header("Typing")]
    public float charactersPerSecond = 30f;
    public bool waitForPlayerInputAfterLine = true;
    public KeyCode continueKey = KeyCode.Space;

    [Header("Speaking Animation")]
    [Tooltip("Optional. Plays when this line starts.")]
    public AnimationClip speakingAnimationClip;

    [Tooltip("Optional. Use this if you prefer typing an animator state name instead of assigning a clip.")]
    public string speakingAnimationStateName;

    [Tooltip("Optional. Plays after the line finishes. Leave blank if you do not need to reset animation.")]
    public string animationStateAfterLine;

    [Header("After Line")]
    public float waitAfterLine = 0.1f;
}

[System.Serializable]
public class SceneActorCommand
{
    public string actorId;

    [Header("Movement")]
    public Vector2 moveDirection = Vector2.right;
    public float moveDistance = 1f;
    public float moveSpeed = 1f;

    [Header("Sprite Flip")]
    [Tooltip("Toggle flips the current SpriteRenderer.flipX value. SetValue forces a specific flipX value and is safer for cutscenes that can reload or replay.")]
    public SpriteFlipMode spriteFlipMode = SpriteFlipMode.Toggle;

    [Tooltip("Used only when Sprite Flip Mode is SetValue. false = normal orientation, true = horizontally flipped.")]
    public bool targetFlipX;

    [Header("Dialogue - Old Optional ScriptableObject")]
    [Tooltip("Still supported for old actions, but for new dialogue use SceneAction.dialogueBeats instead.")]
    public DialogueLine dialogueLine;

    [Header("Animation")]
    public AnimationClip animationClip;

    public enum SpriteFlipMode
    {
        Toggle,
        SetValue
    }
}

[System.Serializable]
public class SceneEndActorCommand
{
    public string actorId;
    public EndBehavior endBehavior;

    [Header("Combat End Command")]
    [Tooltip("Used only when endBehavior is Combat. This gives the cinematic its own enemy pool instead of using the area's fixed/random encounter data.")]
    public CombatInteraction combatInteraction = new();

    public enum EndBehavior
    {
        None,
        Disable,
        Destroy,
        Quest,
        Combat
    }
}
