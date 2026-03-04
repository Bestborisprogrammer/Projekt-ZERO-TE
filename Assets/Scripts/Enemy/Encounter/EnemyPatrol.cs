using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolRadius = 3f;       // Größe der Zone
    public float moveSpeed = 2f;
    public float waitTime = 1.5f;         // Pause zwischen Bewegungen

    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float waitTimer;
    private bool isWaiting = false;

    void Start()
    {
        startPosition = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewTarget();
            }
            return;
        }

        // Bewege Richtung Ziel
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Ziel erreicht?
        if ((Vector2)transform.position == targetPosition)
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void PickNewTarget()
    {
        // Zufälliger Punkt in der Zone
        Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
        targetPosition = startPosition + randomOffset;
    }

    // Zeigt die Zone im Unity Editor als Kreis
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}