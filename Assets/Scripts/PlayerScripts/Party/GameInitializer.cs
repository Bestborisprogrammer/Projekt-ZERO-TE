using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    static bool hasInitialized = false;

    void Awake()
    {
        // Never wipe PlayerPrefs while a save is actively being loaded
        if (SaveManager.IsLoadingSave)
        {
            Debug.Log("[GAME INIT] Skipping PlayerPrefs wipe - a save is being loaded");
            return;
        }

        if (!hasInitialized)
        {
            PlayerPrefs.DeleteAll();
            GearMenuPanel.ResetInitialized();
            hasInitialized = true;
            Debug.Log("Game initialized fresh!");
        }
    }
}