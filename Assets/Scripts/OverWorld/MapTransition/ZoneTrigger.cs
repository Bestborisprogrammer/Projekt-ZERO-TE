using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Destination")]
    public Transform destinationPoint;
    public Vector3 manualDestination;
    public bool useManualDestination = false;

    [Header("Interaction")]
    public bool requireKeyPress = false;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Prompt")]
    public GameObject interactText;

    [Header("NPC Despawn (optional)")]
    public RecruitCutsceneManager recruitCutsceneToNotify;

    private bool isTransitioning = false;
    private bool playerInside = false;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    void Update()
    {
        if (requireKeyPress && playerInside && !isTransitioning)
            if (Input.GetKeyDown(interactKey))
                TeleportPlayer();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;

        if (requireKeyPress)
        {
            if (interactText != null)
                interactText.SetActive(true);
        }
        else
        {
            if (!isTransitioning)
                TeleportPlayer();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        if (interactText != null)
            interactText.SetActive(false);
    }

    void TeleportPlayer()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (interactText != null)
            interactText.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null) movement.enabled = false;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (recruitCutsceneToNotify != null)
            recruitCutsceneToNotify.DespawnNPC();

        Vector3 target = useManualDestination
            ? manualDestination
            : destinationPoint != null
                ? destinationPoint.position
                : transform.position;
        
        AudioManager.Instance?.PlayTransition();
        FadeTransition.Instance.FadeToPosition(target, () =>
        {
            Debug.Log($"Teleported to {target}");
        });

        // fadeDuration is the Inspector value (1.2f)
        // FadeToPosition does: fade in + 0.1s pause + fade out
        // So total = fadeDuration + 0.1 + fadeDuration
        // Unfreeze right as fade out completes
        float totalFade = FadeTransition.Instance.fadeDuration * 1f + 0.15f;
        Invoke(nameof(UnfreezePlayer), totalFade);
        Invoke(nameof(ResetTransition), totalFade + 0.3f);
    }

    void UnfreezePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null) movement.enabled = true;
    }

    void ResetTransition()
    {
        isTransitioning = false;
    }
}