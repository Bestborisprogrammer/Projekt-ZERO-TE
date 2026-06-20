using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    const string SessionFlagKey = "session_initialized_flag";

    void Awake()
    {
        if (SaveManager.IsLoadingSave)
        {
            Debug.Log("[GAME INIT] Skipping PlayerPrefs wipe - a save is being loaded");
            return;
        }

        // FIXED: was using a static bool that could reset between scene loads
        // (Unity domain reload, or simply not being as session-persistent as assumed).
        // PlayerPrefs survives scene reloads reliably, same as every other
        // one-time-trigger flag in this project (DialogueTrigger, etc.)
        if (PlayerPrefs.GetInt(SessionFlagKey, 0) == 0)
        {
            PlayerPrefs.DeleteAll();
            GearMenuPanel.ResetInitialized();

            // Set the flag AFTER DeleteAll, otherwise DeleteAll would erase it immediately
            PlayerPrefs.SetInt(SessionFlagKey, 1);
            PlayerPrefs.Save();

            Debug.Log("Game initialized fresh!");
        }
        else
        {
            Debug.Log("[GAME INIT] Already initialized this session - skipping wipe");
        }
    }
}