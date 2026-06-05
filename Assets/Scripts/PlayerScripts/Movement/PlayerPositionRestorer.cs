using UnityEngine;

public class PlayerPositionRestorer : MonoBehaviour
{
    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerReturnX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerReturnX");
            float y = PlayerPrefs.GetFloat("PlayerReturnY");
            float z = PlayerPrefs.GetFloat("PlayerReturnZ");
            transform.position = new Vector3(x, y, z);

            PlayerPrefs.DeleteKey("PlayerReturnX");
            PlayerPrefs.DeleteKey("PlayerReturnY");
            PlayerPrefs.DeleteKey("PlayerReturnZ");
        }
    }
}