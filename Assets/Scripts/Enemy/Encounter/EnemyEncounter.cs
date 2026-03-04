using UnityEngine;

public class EnemyEncounter : MonoBehaviour
{
    private EnemyController enemyController;

    void Start()
    {
        enemyController = GetComponent<EnemyController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EncounterManager.Instance.StartEncounter(enemyController.enemyData);
        }
    }
}