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
        if (other.CompareTag("Player"))
        {
            PlayerPrefs.SetInt(uniqueID, 1);
            PlayerPrefs.Save();
            EncounterManager.Instance.StartEncounter(enemies);
        }
    }
}