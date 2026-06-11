using UnityEngine;

public class PlayerPositionRestorer : MonoBehaviour
{
    void Start()
    {
        // Combat return takes priority
        if (EncounterManager.PlayerReturnPosition != Vector3.zero)
        {
            transform.position = EncounterManager.PlayerReturnPosition;
            Debug.Log($"[POS] Restored from combat: {transform.position}");
            // Clear so it never restores again
            EncounterManager.PlayerReturnPosition = Vector3.zero;
        }
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