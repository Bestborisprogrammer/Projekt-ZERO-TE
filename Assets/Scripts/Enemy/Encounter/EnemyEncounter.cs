using UnityEngine;
using System.Collections.Generic;

public class EnemyEncounter : MonoBehaviour
{
    public List<EnemyStatsSO> enemies;
    private string uniqueID;

    void Awake()
    {
        uniqueID = $"enemy_{transform.position.x}_{transform.position.y}";
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(uniqueID, 0) == 1)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Freeze player
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        var movement = other.GetComponent<PlayerMovement2D>();
        if (movement != null) movement.enabled = false;

        // Freeze enemy
        var enemyRB = GetComponent<Rigidbody2D>();
        if (enemyRB != null) enemyRB.linearVelocity = Vector2.zero;

        PlayerPrefs.SetInt(uniqueID, 1);
        PlayerPrefs.Save();
        EncounterManager.Instance.StartEncounter(enemies);
    }
}