using UnityEngine;

public class EnemyEncounter : MonoBehaviour
{
    [Header("Encounter Settings")]
    public string enemyID = "slime_01";   // Später für Combat-System nutzbar
    public bool hasBeenDefeated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenDefeated) return;

        if (other.CompareTag("Player"))
        {
            EncounterManager.Instance.StartEncounter(enemyID, gameObject);
        }
    }
}