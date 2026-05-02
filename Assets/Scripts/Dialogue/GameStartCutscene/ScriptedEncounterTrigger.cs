using UnityEngine;

public class ScriptedEncounterTrigger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            cutsceneManager?.TriggerScriptedBattle();
    }
}