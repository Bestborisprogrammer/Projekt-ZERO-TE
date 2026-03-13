using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Sprites")]
    public Sprite spriteIdle;
    public Sprite spriteLeft;
    public Sprite spriteRight;
    public Sprite spriteUp;
    public Sprite spriteDown;

    private Rigidbody2D rb;
    private Vector2 movement;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Restore position after combat
        if (EncounterManager.PlayerReturnPosition != Vector3.zero)
            transform.position = EncounterManager.PlayerReturnPosition;
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;
        UpdateSprite();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    void UpdateSprite()
    {
        if (movement == Vector2.zero)
            sr.sprite = spriteIdle;
        else if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            sr.sprite = movement.x > 0 ? spriteRight : spriteLeft;
        else
            sr.sprite = movement.y > 0 ? spriteUp : spriteDown;
    }
}