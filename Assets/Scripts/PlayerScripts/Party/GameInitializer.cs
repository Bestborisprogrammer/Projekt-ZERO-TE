using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    // This runs once when the game starts fresh from the OverworldScene
    // It clears all defeated enemies so they respawn each Play session
    static bool hasInitialized = false;

    void Awake()
    {
        if (!hasInitialized)
        {
            PlayerPrefs.DeleteAll();
            hasInitialized = false;
        }
    }
}