using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    public void OpenMenu()
    {
        // Save player position before opening menu
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            EncounterManager.PlayerReturnPosition = player.transform.position;

        SceneManager.LoadScene("MenuScene");
    }
}