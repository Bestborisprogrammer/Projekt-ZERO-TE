using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance;

    [Header("Scene Settings")]
    public string combatSceneName = "CombatScene";

    // Daten die zur Combat-Szene weitergegeben werden
    public static string CurrentEnemyID { get; private set; }
    public static GameObject LastEnemyObject { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Bleibt über Szenen hinweg
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartEncounter(string enemyID, GameObject enemyObject)
    {
        CurrentEnemyID = enemyID;
        LastEnemyObject = enemyObject;

        // Optional: kurze Verzögerung / Flash-Effekt hier später einfügen
        Debug.Log($"Encounter gestartet mit: {enemyID}");
        SceneManager.LoadScene(combatSceneName);
    }
}