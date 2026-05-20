using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Destination")]
    public Transform destinationPoint;   // Drag the exit point here
    public Vector3 manualDestination;    // Or set a manual position manually
    public bool useManualDestination = false;

    [Header("Interaction")]
    public bool requireKeyPress = false; // If true, player must press E
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Prompt")]
    public GameObject interactText; // Assign "Press E to Enter" text object

    private bool isTransitioning = false;
    private bool playerInside = false;

    private void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    private void Update()
    {
        if (requireKeyPress && playerInside && !isTransitioning)
        {
            if (Input.GetKeyDown(interactKey))
            {
                TeleportPlayer();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        // Show prompt if interaction required
        if (requireKeyPress)
        {
            if (interactText != null)
                interactText.SetActive(true);
        }
        else
        {
            // Instant transition
            if (!isTransitioning)
                TeleportPlayer();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;

        if (interactText != null)
            interactText.SetActive(false);
    }

    private void TeleportPlayer()
    {
        if (isTransitioning) return;

        isTransitioning = true;

        if (interactText != null)
            interactText.SetActive(false);

        Vector3 target = useManualDestination
            ? manualDestination
            : destinationPoint != null
                ? destinationPoint.position
                : transform.position;

        FadeTransition.Instance.FadeToPosition(target, () =>
        {
            Debug.Log($"Teleported to {target}");
        });

        Invoke(nameof(ResetTransition), 2f);
    }

    private void ResetTransition()
    {
        isTransitioning = false;
    }
}