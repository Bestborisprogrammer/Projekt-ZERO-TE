using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Animator")]
    public Animator animator;

    public static bool ForceFrozen { get; set; } = false;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    // Track the last key pressed so most-recent-held wins
    private bool lastAxisWasHorizontal = false;

    // Track what's currently playing so we don't call Play() every frame
    private string currentClip = "";

    // These must match your animation clip names exactly in the Animator
    private const string IDLE = "EdIdle";
    private const string WALK_UP = "EdWalkUp";
    private const string WALK_DOWN = "EdWalkDown";
    private const string WALK_LEFT = "EdWalkLeft";
    private const string WALK_RIGHT = "EdWalkRight";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (ForceFrozen)
        {
            enabled = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        PlayClip(IDLE);
    }

    void OnEnable()
    {
        if (ForceFrozen) { enabled = false; return; }
        PlayClip(IDLE);
    }

    void OnDisable()
    {
        PlayClip(IDLE);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (ForceFrozen)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            PlayClip(IDLE);
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        // Track which axis was most recently pressed
        bool hPressed = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)
                     || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool vPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
                     || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        if (hPressed && !vPressed) lastAxisWasHorizontal = true;
        if (vPressed && !hPressed) lastAxisWasHorizontal = false;

        bool hasH = Mathf.Abs(x) > 0.01f;
        bool hasV = Mathf.Abs(y) > 0.01f;

        moveInput = new Vector2(x, y).normalized;

        if (!hasH && !hasV)
        {
            // Nothing held — instant idle, same frame
            PlayClip(IDLE);
            return;
        }

        bool useHorizontal;
        if (hasH && hasV)
            useHorizontal = lastAxisWasHorizontal; // last pressed wins
        else
            useHorizontal = hasH; // only one axis active

        if (useHorizontal)
            PlayClip(x > 0 ? WALK_RIGHT : WALK_LEFT);
        else
            PlayClip(y > 0 ? WALK_UP : WALK_DOWN);
    }

    void FixedUpdate()
    {
        if (ForceFrozen) { rb.linearVelocity = Vector2.zero; return; }
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void PlayClip(string clipName)
    {
        if (animator == null) return;
        if (currentClip == clipName) return; // already playing, skip

        currentClip = clipName;
        animator.Play(clipName);
    }
}