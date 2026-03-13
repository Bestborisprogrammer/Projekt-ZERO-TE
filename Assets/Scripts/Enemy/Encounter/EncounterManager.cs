using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance;
    public string combatSceneName = "CombatScene";

    public static List<EnemyStatsSO> CurrentEnemies { get; private set; } = new();
    public static Vector3 PlayerReturnPosition { get; private set; }

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
        // Save player position before leaving
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            PlayerReturnPosition = player.transform.position;

        CurrentEnemies = enemies;
        SceneManager.LoadScene(combatSceneName);
    }
}