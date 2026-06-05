using UnityEngine;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float moveSpeed = 1.5f;
    public float waitTime = 1f;
    public float patrolRadius = 3f;

    private Vector2 startPos;
    private Vector2 targetPos;
    private bool isWaiting = false;
    private bool isFrozen = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        targetPos = GetRandomPoint();
    }

    void Update()
    {
        if (isFrozen) return;
        if (isWaiting) return;

        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, targetPos);

        if (dist < 0.1f)
            StartCoroutine(Wait());
        else
        {
            if (rb != null)
                rb.linearVelocity = dir * moveSpeed;
            else
                transform.position = Vector2.MoveTowards(
                    transform.position, targetPos, moveSpeed * Time.deltaTime);
        }
    }

    IEnumerator Wait()
    {
        isWaiting = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(waitTime);
        targetPos = GetRandomPoint();
        isWaiting = false;
    }

    Vector2 GetRandomPoint()
    {
        Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
        return startPos + randomOffset;
    }

    public void Freeze()
    {
        isFrozen = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}