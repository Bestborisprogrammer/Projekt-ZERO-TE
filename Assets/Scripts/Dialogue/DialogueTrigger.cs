using UnityEngine;
using TMPro;

public enum TriggerType { WalkOver, Interact }

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueSO dialogue;
    public TriggerType triggerType = TriggerType.WalkOver;
    public bool oneTimeOnly = true;

    [Header("Interact Prompt (optional)")]
    public TextMeshProUGUI promptText;
    public string promptMessage = "E - Talk";

    [Header("After Dialogue (optional)")]
    public UnityEngine.Events.UnityEvent onDialogueComplete;

    private bool triggered = false;
    private bool playerNearby = false;
    private string saveKey;

    void Start()
    {
        saveKey = $"dlg_{gameObject.name}_{transform.position.x}_{transform.position.y}";
        if (oneTimeOnly && PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (triggerType == TriggerType.Interact && playerNearby && Input.GetKeyDown(KeyCode.E))
            Trigger();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = true;

        if (triggerType == TriggerType.WalkOver)
            Trigger();
        else if (promptText != null)
        {
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    public void Trigger()
    {
        if (triggered || dialogue == null) return;
        triggered = true;

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        DialogueUI.Instance.StartDialogue(dialogue, () =>
        {
            onDialogueComplete?.Invoke();

            if (oneTimeOnly)
            {
                PlayerPrefs.SetInt(saveKey, 1);
                PlayerPrefs.Save();
                gameObject.SetActive(false);
            }
            else
                triggered = false;
        });
    }
}