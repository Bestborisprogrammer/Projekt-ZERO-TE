using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    static bool hasInitialized = false;

    void Awake()
    {
        if (!hasInitialized)
        {
            PlayerPrefs.DeleteAll();
            hasInitialized = true;
        }
    }
}