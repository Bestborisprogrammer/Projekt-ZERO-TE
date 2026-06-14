using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    public string menuScene = "MenuScene";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            OpenMenu();

        if (Input.GetKeyDown(KeyCode.P))
            OpenPartyStatus();
    }

    void OpenMenu()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // Block if player is frozen
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null && !movement.enabled)
            {
                Debug.Log("[MENU] Blocked – player frozen");
                return;
            }
        }

        // Block if dialogue is open
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen)
        {
            Debug.Log("[MENU] Blocked – dialogue open");
            return;
        }

        // Block if encounter active
        if (EncounterManager.CurrentEnemies != null &&
            EncounterManager.CurrentEnemies.Count > 0)
        {
            Debug.Log("[MENU] Blocked – encounter active");
            return;
        }

        if (player != null)
        {
            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat("PlayerReturnX", pos.x);
            PlayerPrefs.SetFloat("PlayerReturnY", pos.y);
            PlayerPrefs.SetFloat("PlayerReturnZ", pos.z);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene(menuScene);
    }

    void OpenPartyStatus()
    {
        // Same blocks as menu
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null && !movement.enabled)
            {
                Debug.Log("[PARTY] Blocked – player frozen");
                return;
            }
        }

        if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen)
        {
            Debug.Log("[PARTY] Blocked – dialogue open");
            return;
        }

        var partyStatusUI = FindFirstObjectByType<PartyStatusUI>();
        if (partyStatusUI != null)
            partyStatusUI.TogglePanel();
    }
}