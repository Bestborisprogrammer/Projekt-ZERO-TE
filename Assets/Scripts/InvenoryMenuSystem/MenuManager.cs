using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject buttonPanel;
    public GameObject partyPanel;
    public GameObject inventoryPanel;
    public GameObject gearPanel;

    [Header("Scene")]
    public string overworldScene = "OverworldScene";

    void Start()
    {
        ShowButtonPanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            // If any sub panel is open go back to button panel
            if (partyPanel.activeSelf || inventoryPanel.activeSelf || gearPanel.activeSelf)
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
    }

    public void ShowParty()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(true);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(false);
        partyPanel.GetComponent<PartyMenuPanel>()?.Refresh();
    }

    public void ShowInventory()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        gearPanel.SetActive(false);
        inventoryPanel.GetComponent<InventoryMenuPanel>()?.Refresh();
    }

    public void ShowGear()
    {
        buttonPanel.SetActive(false);
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(true);
        gearPanel.GetComponent<GearMenuPanel>()?.Refresh();
    }

    public void ReturnToOverworld()
    {
        // Position is restored by PlayerMovement2D or a dedicated restorer
        SceneManager.LoadScene(overworldScene);
    }
}