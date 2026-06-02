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
}

[System.Serializable]
public class SceneAction
{
    public string actionName;
    public ActionType type;

    [Header("Actor Commands")]
    public List<SceneActorCommand> actorCommands = new();

    [Header("Dialogue")]
    // public Dialogue dialogue;

    [Header("Timing")]
    public float waitAfterAction = 0.5f;

    public enum ActionType
    {
        None,
        Move,
        Animation,
        Dialogue,
        Wait
    }
}

[System.Serializable]
public class SceneActorCommand
{
    public string actorId;

    [Header("Movement")]
    public Vector2 moveDirection = Vector2.right;
    public float moveDistance = 1f;
    public float moveSpeed = 1f;

    [Header("Animation")]
    public AnimationClip animationClip;
}

[System.Serializable]
public class SceneEndActorCommand
{
    public string actorId;
    public EndBehavior endBehavior;

    public enum EndBehavior
    {
        None,
        Disable,
        Destroy
    }
}