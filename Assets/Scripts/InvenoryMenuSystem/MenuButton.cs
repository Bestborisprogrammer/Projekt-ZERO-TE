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
        var partyStatusUI = FindFirstObjectByType<PartyStatusUI>();
        if (partyStatusUI != null)
            partyStatusUI.TogglePanel();
    }
}