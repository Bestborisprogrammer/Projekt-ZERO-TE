using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Animator")]
    public Animator animator;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.down;

    // Track last dominant direction to force transition
    private enum FacingDir { Down, Up, Left, Right }
    private FacingDir currentDir = FacingDir.Down;
    private FacingDir lastDir = FacingDir.Down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
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
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        if (moveInput != Vector2.zero)
            lastMoveDir = moveInput;

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("IsMoving", isMoving);

        // Determine dominant direction
        Vector2 dir = isMoving ? moveInput : lastMoveDir;

        // Pick dominant axis – horizontal wins if equal
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);

        if (absX >= absY)
        {
            // Horizontal dominant
            if (dir.x < 0)
            {
                currentDir = FacingDir.Left;
                animator.SetFloat("MoveX", -1);
                animator.SetFloat("MoveY", 0);
            }
            else
            {
                currentDir = FacingDir.Right;
                animator.SetFloat("MoveX", 1);
                animator.SetFloat("MoveY", 0);
            }
        }
        else
        {
            // Vertical dominant
            if (dir.y < 0)
            {
                currentDir = FacingDir.Down;
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", -1);
            }
            else
            {
                currentDir = FacingDir.Up;
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 1);
            }
        }

        // Force animator to re-evaluate when direction changes
        if (currentDir != lastDir)
        {
            lastDir = currentDir;
            // Briefly reset IsMoving to force transition re-check
            if (isMoving)
            {
                animator.SetBool("IsMoving", false);
                animator.Update(0f);
                animator.SetBool("IsMoving", true);
            }
        }
    }
}