using UnityEngine;

public class ScriptedEventTrigger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        cutsceneManager?.StartCutscene();
        // Disable self so it never fires again this session
        gameObject.SetActive(false);
    }
}