using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject buttonPanel;
    public GameObject partyPanel;
    public GameObject inventoryPanel;
    public GameObject gearPanel;
    public GameObject savePanel;

    [Header("Main Menu Confirmation Popup")]
    public GameObject mainMenuConfirmPopup;

    [Header("Scene")]
    public string overworldScene = "overworldScene";
    public string mainMenuScene = "MainMenu";

    [Header("Transition")]
    public UnityEngine.UI.Image transitionOverlay;
    public float transitionDuration = 0.4f;

    void Start()
    {
        ShowButtonPanel();

        if (mainMenuConfirmPopup != null)
            mainMenuConfirmPopup.SetActive(false);

        // Make sure overlay starts invisible
        if (transitionOverlay != null)
            transitionOverlay.color = new Color(0, 0, 0, 0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (mainMenuConfirmPopup != null && mainMenuConfirmPopup.activeSelf)
            {
                mainMenuConfirmPopup.SetActive(false);
                return;
            }
            if (partyPanel.activeSelf || inventoryPanel.activeSelf ||
                gearPanel.activeSelf || savePanel.activeSelf)
                ShowButtonPanel();
            else
                ReturnToOverworld();
        }
    }

    public void ShowButtonPanel()
    {
        buttonPanel.SetActive(true);
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(false);
        savePanel.SetActive(false);
    }

    public void ShowParty()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(true);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(false);
        savePanel.SetActive(false);
        partyPanel.GetComponent<PartyMenuPanel>()?.Refresh();
    }

    public void ShowInventory()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        gearPanel.SetActive(false);
        savePanel.SetActive(false);
        inventoryPanel.GetComponent<InventoryMenuPanel>()?.Refresh();
    }

    public void ShowGear()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(true);
        savePanel.SetActive(false);
        gearPanel.GetComponent<GearMenuPanel>()?.Refresh();
    }

    public void ShowSave()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(false);
        savePanel.SetActive(true);
    }

    public void ReturnToOverworld()
    {
        Debug.Log($"[MENU] Returning to overworld: {overworldScene}");
        StartCoroutine(TransitionThenLoad(overworldScene));
    }

    // Called by the "Main Menu" button in the pause menu
    public void ShowMainMenuConfirm()
    {
        if (mainMenuConfirmPopup != null)
            mainMenuConfirmPopup.SetActive(true);
    }

    // Called by "Yes" in the confirm popup
    public void ConfirmGoToMainMenu()
    {
        if (mainMenuConfirmPopup != null)
            mainMenuConfirmPopup.SetActive(false);
        StartCoroutine(TransitionThenLoad(mainMenuScene));
    }

    // Called by "No" / "Cancel" in the confirm popup
    public void CancelMainMenu()
    {
        if (mainMenuConfirmPopup != null)
            mainMenuConfirmPopup.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("[MENU] Quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator TransitionThenLoad(string scene)
    {
        // Fade to black
        if (transitionOverlay != null)
        {
            float t = 0f;
            transitionOverlay.gameObject.SetActive(true);
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                transitionOverlay.color = new Color(0, 0, 0, Mathf.Clamp01(t / transitionDuration));
                yield return null;
            }
            transitionOverlay.color = Color.black;
        }
        else yield return new WaitForSeconds(0.1f);

        SceneManager.LoadScene(scene);
    }
}