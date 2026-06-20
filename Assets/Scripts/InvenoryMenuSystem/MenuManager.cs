using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject buttonPanel;
    public GameObject partyPanel;
    public GameObject inventoryPanel;
    public GameObject gearPanel;
    public GameObject savePanel;

    [Header("Scene")]
    public string overworldScene = "overworldScene";

    void Start()
    {
        ShowButtonPanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
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
        Debug.Log($"[MENU] Returning to overworld via scene replace: {overworldScene}");
        SceneManager.LoadScene(overworldScene);
    }

    // NEW: Quit button
    public void QuitGame()
    {
        Debug.Log("[MENU] Quit button pressed");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}