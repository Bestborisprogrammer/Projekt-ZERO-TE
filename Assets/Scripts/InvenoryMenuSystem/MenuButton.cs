using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    public void OpenMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}