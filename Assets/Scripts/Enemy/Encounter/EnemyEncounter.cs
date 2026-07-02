using UnityEngine;
using System.Collections.Generic;

public class EnemyEncounter : MonoBehaviour
{
    public List<EnemyStatsSO> enemies;
    public string uniqueID;

    void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = $"enemy_{transform.position.x}_{transform.position.y}";

        TrackedPlayerPrefsKeys.Register(uniqueID);
        Debug.Log($"[ENEMY ENCOUNTER] Awake - uniqueID: {uniqueID}");
    }

    void Start()
    {
        int val = PlayerPrefs.GetInt(uniqueID, 0);
        Debug.Log($"[ENEMY ENCOUNTER] Start - {uniqueID} = {val}");
        if (val == 1)
        {
            Debug.Log($"[ENEMY ENCOUNTER] Already defeated, destroying");
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var movement = other.GetComponent<PlayerMovement2D>();
        if (movement != null) movement.enabled = false;
        var playerRB = other.GetComponent<Rigidbody2D>();
        if (playerRB != null) playerRB.linearVelocity = Vector2.zero;

        var patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.Freeze();

        var enemyRB = GetComponent<Rigidbody2D>();
        if (enemyRB != null) enemyRB.linearVelocity = Vector2.zero;

        EncounterManager.LastEncounterTriggerID = uniqueID;
        EncounterManager.LastEncounterWasScripted = false;
        EncounterManager.LastEncounterWasRecruit = false;

        Debug.Log($"[ENEMY ENCOUNTER] Triggered - setting {uniqueID} = 1");
        PlayerPrefs.SetInt(uniqueID, 1);
        PlayerPrefs.Save();
        EncounterManager.Instance.StartEncounter(enemies);
    }
}