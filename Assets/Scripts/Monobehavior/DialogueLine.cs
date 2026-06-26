using UnityEngine;

[CreateAssetMenu(fileName = "DialogueLine", menuName = "Scriptable Objects/DialogueLine")]
public class DialogueLine : ScriptableObject
{
    [Header("Who is speaking")]
    public string characterID;

    [Header("Text")]
    [TextArea(2, 6)]
    public string line;

    [Header("UI")]
    public GameObject speechBubblePrefab;

    [Header("Typing")]
    public float charactersPerSecond = 30f;
    public bool waitForPlayerInputAfterLine = true;
    public KeyCode continueKey = KeyCode.Space;
}
