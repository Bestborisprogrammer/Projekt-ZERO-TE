using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject partyPanel;
    public GameObject inventoryPanel;
    public GameObject gearPanel;

    [Header("Scene")]
    public string overworldScene = "OverworldScene";

    void Start()
    {
        ShowParty();
    }

    public void ShowParty()
    {
        partyPanel.SetActive(true);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(false);
        partyPanel.GetComponent<PartyMenuPanel>()?.Refresh();
    }

    public void ShowInventory()
    {
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        gearPanel.SetActive(false);
        inventoryPanel.GetComponent<InventoryMenuPanel>()?.Refresh();
    }

    public void ShowGear()
    {
        partyPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        gearPanel.SetActive(true);
        gearPanel.GetComponent<GearMenuPanel>()?.Refresh();
    }

    public void ReturnToOverworld()
    {
        SceneManager.LoadScene(overworldScene);
    }
}