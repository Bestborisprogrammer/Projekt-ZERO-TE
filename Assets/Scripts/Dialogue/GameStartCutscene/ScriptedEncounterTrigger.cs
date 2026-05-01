using UnityEngine;

public class ScriptedEncounterTrigger : MonoBehaviour
{
    private CutsceneManager cutsceneManager;

    void Start()
    {
        cutsceneManager = Object.FindFirstObjectByType<CutsceneManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            cutsceneManager?.TriggerScriptedBattle();
    }
}