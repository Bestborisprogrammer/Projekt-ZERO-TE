using UnityEngine;

public class ScriptedEncounterTrigger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Freeze player
        var playerRB = other.GetComponent<Rigidbody2D>();
        if (playerRB != null)
        {
            playerRB.linearVelocity = Vector2.zero;
            playerRB.bodyType = RigidbodyType2D.Static;
        }
        var playerMovement = other.GetComponent<PlayerMovement2D>();
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Freeze monster
        var monsterRB = GetComponent<Rigidbody2D>();
        if (monsterRB != null)
        {
            monsterRB.linearVelocity = Vector2.zero;
            monsterRB.bodyType = RigidbodyType2D.Static;
        }

        cutsceneManager?.TriggerScriptedBattle();
    }
}