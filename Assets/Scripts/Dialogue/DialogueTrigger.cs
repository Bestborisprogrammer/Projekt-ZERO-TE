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

    [Header("Linked Systems (optional)")]
    public CutsceneManager linkedCutsceneManager;
    public RecruitCutsceneManager linkedRecruitCutscene;
    public ResonanceCutsceneManager linkedResonanceCutscene;

    private bool triggered = false;
    private bool playerNearby = false;
    private string saveKey;

    void Awake()
    {
        // Build saveKey here so it exists for both Register AND Start's check
        saveKey = $"dlg_{gameObject.name}_{transform.position.x}_{transform.position.y}";
        TrackedPlayerPrefsKeys.Register(saveKey);
        Debug.Log($"[DIALOGUE TRIGGER] Awake - saveKey: {saveKey}");
    }

    void Start()
    {
        int flagValue = PlayerPrefs.GetInt(saveKey, 0);
        Debug.Log($"[DIALOGUE TRIGGER] Start - {saveKey} = {flagValue}");

        if (oneTimeOnly && flagValue == 1)
        {
            Debug.Log($"[DIALOGUE TRIGGER] Already triggered - disabling {gameObject.name}");
            gameObject.SetActive(false);
            return;
        }

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (triggerType == TriggerType.Interact &&
            playerNearby &&
            Input.GetKeyDown(KeyCode.E))
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
            linkedCutsceneManager?.StartMonsterSequence();
            linkedRecruitCutscene?.StartRecruitCutscene();

            if (oneTimeOnly)
            {
                Debug.Log($"[DIALOGUE TRIGGER] Marking complete: {saveKey} = 1");
                PlayerPrefs.SetInt(saveKey, 1);
                PlayerPrefs.Save();
                gameObject.SetActive(false);
            }
            else
                triggered = false;
        });

        linkedResonanceCutscene?.StartResonanceCutscene();
    }
}