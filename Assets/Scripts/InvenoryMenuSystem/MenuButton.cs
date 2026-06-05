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

        // DEBUG
        if (Input.GetKeyDown(KeyCode.F1))
            DumpMenuState();
    }

    void OpenMenu()
    {
        Debug.Log("[MENU] Attempting to open menu...");

        // Block menu during active cutscenes or encounters
        if (IsCutsceneActive())
        {
            Debug.Log("[MENU] Blocked – cutscene active");
            return;
        }

        if (IsEncounterStarting())
        {
            Debug.Log("[MENU] Blocked – encounter starting");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Also block if player movement is disabled (frozen)
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null && !movement.enabled)
            {
                Debug.Log("[MENU] Blocked – player frozen");
                return;
            }

            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat("PlayerReturnX", pos.x);
            PlayerPrefs.SetFloat("PlayerReturnY", pos.y);
            PlayerPrefs.SetFloat("PlayerReturnZ", pos.z);
            PlayerPrefs.Save();

            Debug.Log($"[MENU] Saved player position: {pos}");
        }

        Debug.Log("[MENU] Opening menu scene...");
        SceneManager.LoadScene(menuScene);
    }

    bool IsCutsceneActive()
    {
        var player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement2D>();

            var cutscene = FindFirstObjectByType<CutsceneManager>();
            if (cutscene != null)
            {
                if (movement != null && !movement.enabled)
                {
                    Debug.Log("[MENU DEBUG] CutsceneManager exists and player movement is disabled.");
                    return true;
                }
            }

            var recruitCutscene = FindFirstObjectByType<RecruitCutsceneManager>();
            if (recruitCutscene != null)
            {
                if (movement != null && !movement.enabled)
                {
                    Debug.Log("[MENU DEBUG] RecruitCutsceneManager exists and player movement is disabled.");
                    return true;
                }
            }
        }

        return false;
    }

    bool IsEncounterStarting()
    {
        bool blocked =
            EncounterManager.CurrentEnemies != null &&
            EncounterManager.CurrentEnemies.Count > 0 &&
            SceneManager.GetActiveScene().name != "OverworldScene";

        if (blocked)
        {
            Debug.Log(
                $"[MENU DEBUG] Encounter block active. " +
                $"Enemy Count: {EncounterManager.CurrentEnemies.Count}, " +
                $"Scene: {SceneManager.GetActiveScene().name}"
            );
        }

        return blocked;
    }

    void OpenPartyStatus()
    {
        var partyStatusUI = FindFirstObjectByType<PartyStatusUI>();
        if (partyStatusUI != null)
            partyStatusUI.TogglePanel();
    }

    void DumpMenuState()
    {
        Debug.Log("========== MENU DEBUG ==========");

        Debug.Log($"Scene: {SceneManager.GetActiveScene().name}");

        if (EncounterManager.CurrentEnemies == null)
        {
            Debug.Log("CurrentEnemies = NULL");
        }
        else
        {
            Debug.Log($"CurrentEnemies Count = {EncounterManager.CurrentEnemies.Count}");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.Log("Player not found!");
            return;
        }

        var movement = player.GetComponent<PlayerMovement2D>();

        if (movement == null)
        {
            Debug.Log("PlayerMovement2D missing!");
        }
        else
        {
            Debug.Log($"PlayerMovement2D Enabled = {movement.enabled}");
        }

        Debug.Log($"CutsceneManager Found = {FindFirstObjectByType<CutsceneManager>() != null}");
        Debug.Log($"RecruitCutsceneManager Found = {FindFirstObjectByType<RecruitCutsceneManager>() != null}");

        Debug.Log($"IsCutsceneActive() = {IsCutsceneActive()}");
        Debug.Log($"IsEncounterStarting() = {IsEncounterStarting()}");

        Debug.Log("================================");
    }
}