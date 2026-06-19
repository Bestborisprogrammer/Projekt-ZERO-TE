using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveMenuPanel : MonoBehaviour
{
    [Header("Mode")]
    public bool isLoadOnlyMode = false; // true = main menu "Continue", false = in-game save menu

    [Header("Slots")]
    public Transform slotsParent;
    public GameObject slotPrefab;

    [Header("Confirmation Popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private List<SaveSlotUI> slotUIs = new();
    private int pendingSlot = -99;
    private bool pendingIsOverwrite = false;

    void OnEnable()
    {
        BuildSlots();
        confirmPopup.SetActive(false);
    }

    void BuildSlots()
    {
        foreach (Transform child in slotsParent)
            Destroy(child.gameObject);
        slotUIs.Clear();

        // Autosave slot first
        GameObject autoObj = Instantiate(slotPrefab, slotsParent);
        var autoUI = autoObj.GetComponent<SaveSlotUI>();
        autoUI.Setup(SaveManager.AutoSaveSlot, this);
        slotUIs.Add(autoUI);

        // 10 manual slots
        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsParent);
            var ui = obj.GetComponent<SaveSlotUI>();
            ui.Setup(i, this);
            slotUIs.Add(ui);
        }
    }

    public void OnSlotClicked(int slot)
    {
        Debug.Log($"[SAVE MENU] Slot clicked: {slot}, isLoadOnlyMode={isLoadOnlyMode}");

        if (isLoadOnlyMode)
        {
            // Main menu "Continue" flow – only load, no save option
            if (!SaveManager.Instance.SlotExists(slot))
            {
                Debug.Log("[SAVE MENU] Empty slot in load-only mode, ignoring");
                return;
            }
            SaveManager.Instance.LoadFromSlot(slot);
            return;
        }

        // In-game save menu: ask save vs overwrite
        bool exists = SaveManager.Instance.SlotExists(slot);
        pendingSlot = slot;
        pendingIsOverwrite = exists;

        confirmPopup.SetActive(true);
        confirmText.text = exists
            ? "This slot already has a save.\nOverwrite it?"
            : "Save your game here?";

        confirmYesButton.onClick.RemoveAllListeners();
        confirmYesButton.onClick.AddListener(ConfirmSave);

        confirmNoButton.onClick.RemoveAllListeners();
        confirmNoButton.onClick.AddListener(() => confirmPopup.SetActive(false));
    }

    void ConfirmSave()
    {
        Debug.Log($"[SAVE MENU] Confirmed save to slot {pendingSlot}, wasOverwrite={pendingIsOverwrite}");
        SaveManager.Instance.SaveToSlot(pendingSlot);
        confirmPopup.SetActive(false);
        BuildSlots(); // refresh display
    }
}