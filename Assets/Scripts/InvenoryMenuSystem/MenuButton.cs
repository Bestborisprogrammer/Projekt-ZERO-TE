using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    public string menuScene = "MenuScene";
    private Vector3 savedPosition;

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
            PlayerPrefs.SetString("PlayerReturnPos",
                $"{player.transform.position.x},{player.transform.position.y},{player.transform.position.z}");
        SceneManager.LoadScene(menuScene);
    }

    void OpenPartyStatus()
    {
        var partyStatusUI = FindFirstObjectByType<PartyStatusUI>();
        if (partyStatusUI != null)
            partyStatusUI.TogglePanel();
    }
}