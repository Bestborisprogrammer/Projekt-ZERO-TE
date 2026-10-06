using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Destination")]
    public Transform destinationPoint;
    public Vector3 manualDestination;
    public bool useManualDestination = false;

    [Header("Return Teleport")]
    [Tooltip("Allows the destination area to teleport the player back to this ZoneTrigger.")]
    public bool destinationCanTeleportBack = false;

    [Tooltip("The trigger collider at the destination that should teleport the player back.")]
    public Collider2D destinationReturnCollider;

    [Tooltip("How long the player must wait before another teleport can happen.")]
    public float teleportCooldown = 1f;

    [Header("Interaction")]
    public bool requireKeyPress = false;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Prompt")]
    public GameObject interactText;

    [Header("NPC Despawn (optional)")]
    public RecruitCutsceneManager recruitCutsceneToNotify;

    private bool isTransitioning = false;
    private bool playerInside = false;

    // Cooldown prevents instant back-and-forth teleporting.
    private float teleportCooldownTimer = 0f;

    // Used to tell the destination collider whether it should act as
    // the return teleport for this ZoneTrigger.
    private static ZoneTrigger activeReturnZone;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);

        // If return teleporting is enabled, register this ZoneTrigger
        // as the owner of the destination return collider.
        if (destinationCanTeleportBack && destinationReturnCollider != null)
        {
            DestinationReturnTeleport.Register(
                destinationReturnCollider,
                this
            );
        }
    }

    void Update()
    {
        if (teleportCooldownTimer > 0f)
            teleportCooldownTimer -= Time.deltaTime;

        if (requireKeyPress && playerInside && !isTransitioning)
        {
            if (teleportCooldownTimer <= 0f &&
                Input.GetKeyDown(interactKey))
            {
                TeleportPlayer();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (teleportCooldownTimer > 0f)
        {
            if (interactText != null)
                interactText.SetActive(false);

            return;
        }

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
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (interactText != null)
            interactText.SetActive(false);
    }

    public void TeleportPlayer()
    {
        if (isTransitioning)
            return;

        if (teleportCooldownTimer > 0f)
            return;

        isTransitioning = true;

        if (interactText != null)
            interactText.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();

            if (movement != null)
                movement.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();

            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        if (recruitCutsceneToNotify != null)
            recruitCutsceneToNotify.DespawnNPC();

        Vector3 target = useManualDestination
            ? manualDestination
            : destinationPoint != null
                ? destinationPoint.position
                : transform.position;

        // Start cooldown immediately.
        teleportCooldownTimer = teleportCooldown;

        AudioManager.Instance?.PlayTransition();

        FadeTransition.Instance.FadeToPosition(target, () =>
        {
            Debug.Log($"Teleported to {target}");
        });

        float totalFade =
            FadeTransition.Instance.fadeDuration + 0.15f;

        Invoke(nameof(UnfreezePlayer), totalFade);
        Invoke(nameof(ResetTransition), totalFade + 0.3f);
    }

    // Called by DestinationReturnTeleport when the player enters
    // the destination collider.
    public void TeleportBack()
    {
        if (isTransitioning)
            return;

        if (teleportCooldownTimer > 0f)
            return;

        // The return teleport target is this ZoneTrigger's own position.
        Vector3 target = transform.position;

        isTransitioning = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();

            if (movement != null)
                movement.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();

            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        // Start cooldown before moving the player.
        teleportCooldownTimer = teleportCooldown;

        AudioManager.Instance?.PlayTransition();

        FadeTransition.Instance.FadeToPosition(target, () =>
        {
            Debug.Log($"Teleported back to {target}");
        });

        float totalFade =
            FadeTransition.Instance.fadeDuration + 0.15f;

        Invoke(nameof(UnfreezePlayer), totalFade);
        Invoke(nameof(ResetTransition), totalFade + 0.3f);
    }

    void UnfreezePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        var movement = player.GetComponent<PlayerMovement2D>();

        if (movement != null)
            movement.enabled = true;
    }

    void ResetTransition()
    {
        isTransitioning = false;
    }

    void OnDestroy()
    {
        if (destinationReturnCollider != null)
        {
            DestinationReturnTeleport.Unregister(
                destinationReturnCollider
            );
        }
    }
}


/// <summary>
/// Handles the collider at the destination and sends the player
/// back to the ZoneTrigger that owns that collider.
/// </summary>
public class DestinationReturnTeleport : MonoBehaviour
{
    private static readonly System.Collections.Generic.Dictionary<
        Collider2D,
        ZoneTrigger
    > registeredColliders =
        new System.Collections.Generic.Dictionary<
            Collider2D,
            ZoneTrigger
        >();

    public static void Register(
        Collider2D collider,
        ZoneTrigger zone
    )
    {
        if (collider == null || zone == null)
            return;

        if (registeredColliders.ContainsKey(collider))
        {
            registeredColliders[collider] = zone;
        }
        else
        {
            registeredColliders.Add(collider, zone);
        }

        // Add this helper component to the destination object.
        DestinationReturnTeleport helper =
            collider.GetComponent<DestinationReturnTeleport>();

        if (helper == null)
        {
            helper =
                collider.gameObject.AddComponent<DestinationReturnTeleport>();
        }

        helper.targetZone = zone;
    }

    public static void Unregister(Collider2D collider)
    {
        if (collider == null)
            return;

        if (registeredColliders.ContainsKey(collider))
            registeredColliders.Remove(collider);
    }

    private ZoneTrigger targetZone;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (targetZone == null)
            return;

        targetZone.TeleportBack();
    }
}
