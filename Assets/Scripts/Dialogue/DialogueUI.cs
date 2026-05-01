using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI")]
    public GameObject dialogueBox;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public GameObject portraitContainer;

    private bool isTyping = false;
    private bool skipRequested = false;
    private bool waitingForInput = false;
    private System.Action onComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        dialogueBox.SetActive(false);
    }

    void Update()
    {
        if (!dialogueBox.activeSelf) return;

        if (Input.anyKeyDown &&
            !Input.GetMouseButtonDown(0) &&
            !Input.GetMouseButtonDown(1))
        {
            if (isTyping)
                skipRequested = true;
            else if (waitingForInput)
                waitingForInput = false;
        }
    }

    public void StartDialogue(DialogueSO dialogue, System.Action callback = null)
    {
        StartDialogueRange(dialogue, 0, dialogue.lines.Count - 1, callback);
    }

    public void StartDialogueRange(DialogueSO dialogue, int from, int to, System.Action callback = null)
    {
        onComplete = callback;
        dialogueBox.SetActive(true);

        // Freeze player
        SetPlayerMovement(false);

        StartCoroutine(PlayDialogueRange(dialogue, from, to));
    }

    IEnumerator PlayDialogueRange(DialogueSO dialogue, int from, int to)
    {
        to = Mathf.Min(to, dialogue.lines.Count - 1);

        for (int i = from; i <= to; i++)
        {
            var line = dialogue.lines[i];

            speakerText.text = line.speakerName;

            if (line.portrait != null)
            {
                portraitContainer?.SetActive(true);
                if (portraitImage != null)
                    portraitImage.sprite = line.portrait;
            }
            else
            {
                portraitContainer?.SetActive(false);
            }

            yield return StartCoroutine(TypeText(line));

            if (line.autoAdvance)
                yield return new WaitForSeconds(line.autoAdvanceDelay);
            else
            {
                waitingForInput = true;
                yield return new WaitUntil(() => !waitingForInput);
            }
        }

        EndDialogue();
    }

    IEnumerator TypeText(DialogueLine line)
    {
        isTyping = true;
        skipRequested = false;
        dialogueText.text = "";

        foreach (char c in line.text)
        {
            if (skipRequested)
            {
                dialogueText.text = line.text;
                break;
            }
            dialogueText.text += c;
            yield return new WaitForSeconds(line.typingSpeed);
        }

        isTyping = false;
    }

    void EndDialogue()
    {
        dialogueBox.SetActive(false);
        SetPlayerMovement(true);
        onComplete?.Invoke();
        onComplete = null;
    }

    void SetPlayerMovement(bool enabled)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && !enabled)
            rb.linearVelocity = Vector2.zero;

        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
            movement.enabled = enabled;
    }
}