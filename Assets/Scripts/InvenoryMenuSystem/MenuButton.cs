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

        // Block if encounter is active
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
            Debug.Log($"[MENU] Opened. Saved position: {pos}");
        }

        SceneManager.LoadScene(menuScene);
    }

    void OpenPartyStatus()
    {
        var partyStatusUI = FindFirstObjectByType<PartyStatusUI>();
        if (partyStatusUI != null)
            partyStatusUI.TogglePanel();
    }
}