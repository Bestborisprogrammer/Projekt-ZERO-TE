using UnityEngine;

public class PlayerPositionRestorer : MonoBehaviour
{
    void Start()
    {
        // Check combat return position first
        if (EncounterManager.PlayerReturnPosition != Vector3.zero)
        {
            transform.position = EncounterManager.PlayerReturnPosition;
            Debug.Log($"[POS] Restored from combat: {transform.position}");
            // Don't clear it here – EncounterManager handles that
        }
        // Then check menu return position
        else if (PlayerPrefs.HasKey("PlayerReturnX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerReturnX");
            float y = PlayerPrefs.GetFloat("PlayerReturnY");
            float z = PlayerPrefs.GetFloat("PlayerReturnZ");
            transform.position = new Vector3(x, y, z);
            Debug.Log($"[POS] Restored from menu: {transform.position}");

            PlayerPrefs.DeleteKey("PlayerReturnX");
            PlayerPrefs.DeleteKey("PlayerReturnY");
            PlayerPrefs.DeleteKey("PlayerReturnZ");
        }
    }
}