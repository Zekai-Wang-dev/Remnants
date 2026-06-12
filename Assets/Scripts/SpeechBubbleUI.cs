using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubbleUI : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Optional")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private LayoutElement layoutElement;

    [Header("Size")]
    [SerializeField] private float maxWidth = 450f;
    [SerializeField] private Vector2 padding = new Vector2(50f, 35f);

    [Header("Follow Actor")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    private Transform followTarget;
    private Camera mainCamera;

    private void Awake()
    {
        if (bubbleRect == null)
        {
            bubbleRect = GetComponent<RectTransform>();
        }

        if (dialogueText == null)
        {
            dialogueText = GetComponentInChildren<TMP_Text>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    public IEnumerator Play(DialogueLine dialogueLine, Transform targetOverride = null)
    {
        if (dialogueLine == null)
        {
            Destroy(gameObject);
            yield break;
        }

        followTarget = targetOverride;

        if (speakerNameText != null)
        {
            speakerNameText.text = dialogueLine.characterID;
            speakerNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(dialogueLine.characterID));
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        yield return StartCoroutine(TypeLine(dialogueLine.line, dialogueLine.charactersPerSecond));

        if (dialogueLine.waitForPlayerInputAfterLine)
        {
            yield return StartCoroutine(WaitForContinue(dialogueLine.continueKey));
        }

        Destroy(gameObject);
    }

    private IEnumerator TypeLine(string fullLine, float charactersPerSecond)
    {
        if (dialogueText == null)
        {
            yield break;
        }

        fullLine ??= "";
        dialogueText.text = "";
        ResizeBubble();

        float delay = charactersPerSecond <= 0f ? 0f : 1f / charactersPerSecond;

        for (int i = 0; i < fullLine.Length; i++)
        {
            dialogueText.text += fullLine[i];
            ResizeBubble();

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator WaitForContinue(KeyCode continueKey)
    {
        while (!Input.GetKeyDown(continueKey) && !Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
    }

    private void ResizeBubble()
    {
        if (dialogueText == null || bubbleRect == null)
        {
            return;
        }

        Vector2 preferredTextSize = dialogueText.GetPreferredValues(dialogueText.text, maxWidth, 0f);

        float width = Mathf.Min(preferredTextSize.x, maxWidth) + padding.x;
        float height = preferredTextSize.y + padding.y;

        bubbleRect.sizeDelta = new Vector2(width, height);

        if (layoutElement != null)
        {
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
        }
    }

    private void FollowTarget()
    {
        if (followTarget == null || bubbleRect == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        bubbleRect.position = mainCamera.WorldToScreenPoint(followTarget.position + worldOffset);
    }
}
