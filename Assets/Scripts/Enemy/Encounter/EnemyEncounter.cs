using UnityEngine;

public class EnemyEncounter : MonoBehaviour
{
    private EnemyController enemyController;
    public static string DefeatedEnemyID;

    void Start()
    {
        enemyController = GetComponent<EnemyController>();

        // Wurde dieser Enemy schon besiegt? Dann löschen
        if (DefeatedEnemyID == enemyController.enemyData.enemyName)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EncounterManager.Instance.StartEncounter(enemyController.enemyData);
        }
    }
}