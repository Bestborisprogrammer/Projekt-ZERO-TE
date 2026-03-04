using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance;
    public string combatSceneName = "CombatScene";

    public static EnemyStatsSO CurrentEnemy { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    public void StartEncounter(EnemyStatsSO enemyData)
    {
        CurrentEnemy = enemyData;
        Debug.Log($"Encounter mit: {enemyData.enemyName}");
        SceneManager.LoadScene(combatSceneName);
    }
}