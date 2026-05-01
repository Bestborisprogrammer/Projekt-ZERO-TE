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
        onComplete = callback;
        dialogueBox.SetActive(true);

        // Freeze player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null) movement.enabled = false;
        }

        StartCoroutine(PlayDialogue(dialogue));
    }

    IEnumerator PlayDialogue(DialogueSO dialogue)
    {
        foreach (var line in dialogue.lines)
        {
            // Speaker name
            speakerText.text = line.speakerName;

            // Portrait
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

            // Typewriter effect
            yield return StartCoroutine(TypeText(line));

            // Wait for input or auto advance
            if (line.autoAdvance)
            {
                yield return new WaitForSeconds(line.autoAdvanceDelay);
            }
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

        // Unfreeze player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null) movement.enabled = true;
        }

        onComplete?.Invoke();
    }
}