using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Animator")]
    public Animator animator;

    // Static flag – persists across scene loads
    public static bool ForceFrozen { get; set; } = false;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.down;

    private enum FacingDir { Down, Up, Left, Right }
    private FacingDir currentDir = FacingDir.Down;
    private FacingDir lastDir = FacingDir.Down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        // If a cutscene flagged a force freeze, disable immediately
        if (ForceFrozen)
        {
            enabled = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            Debug.Log("[PLAYER] ForceFrozen on start – movement disabled");
        }
    }

    void OnEnable()
    {
        if (ForceFrozen)
        {
            enabled = false;
            return;
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", -1);
        }
    }

    void OnDisable()
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", -1);
        }
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (ForceFrozen)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        if (moveInput != Vector2.zero)
            lastMoveDir = moveInput;

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (ForceFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("IsMoving", isMoving);

        Vector2 dir = isMoving ? moveInput : lastMoveDir;
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);

        if (absX >= absY)
        {
            currentDir = dir.x < 0 ? FacingDir.Left : FacingDir.Right;
            animator.SetFloat("MoveX", dir.x < 0 ? -1 : 1);
            animator.SetFloat("MoveY", 0);
        }
        else
        {
            currentDir = dir.y < 0 ? FacingDir.Down : FacingDir.Up;
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", dir.y < 0 ? -1 : 1);
        }

        if (currentDir != lastDir)
        {
            lastDir = currentDir;
            if (isMoving)
            {
                animator.SetBool("IsMoving", false);
                animator.Update(0f);
                animator.SetBool("IsMoving", true);
            }
        }
    }
}