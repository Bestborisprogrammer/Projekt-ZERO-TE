using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance;
    public string combatSceneName = "CombatScene";

    public static List<EnemyStatsSO> CurrentEnemies { get; private set; } = new();
    public static Vector3 PlayerReturnPosition { get; set; } // changed to set

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    public void StartEncounter(List<EnemyStatsSO> enemies)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            PlayerReturnPosition = player.transform.position;

        CurrentEnemies = enemies;
        SceneManager.LoadScene(combatSceneName);
    }
}