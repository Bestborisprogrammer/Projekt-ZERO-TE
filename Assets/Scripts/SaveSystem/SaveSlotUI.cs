using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI slotTitleText;
    public TextMeshProUGUI playtimeText;
    public TextMeshProUGUI dateText;
    public Transform memberPortraitParent;
    public GameObject memberPortraitPrefab;
    public GameObject emptySlotLabel;
    public Button slotButton;

    private int slotIndex;
    private SaveMenuPanel parentPanel;

    public void Setup(int index, SaveMenuPanel panel)
    {
        slotIndex = index;
        parentPanel = panel;
        Refresh();

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => parentPanel.OnSlotClicked(slotIndex));
        }
    }

    public void Refresh()
    {
        if (memberPortraitParent != null)
        {
            foreach (Transform child in memberPortraitParent)
                Destroy(child.gameObject);
        }

        // Instance now auto-creates itself, this will never be null
        var data = SaveManager.Instance.LoadSlotPreview(slotIndex);

        bool isAuto = slotIndex == SaveManager.AutoSaveSlot;

        if (data == null || data.isEmpty)
        {
            if (slotTitleText != null)
                slotTitleText.text = isAuto ? "Autosave (Empty)" : $"Save Slot {slotIndex + 1} (Empty)";
            if (playtimeText != null) playtimeText.text = "";
            if (dateText != null) dateText.text = "";
            if (emptySlotLabel != null) emptySlotLabel.SetActive(true);
            return;
        }

        if (emptySlotLabel != null) emptySlotLabel.SetActive(false);

        if (slotTitleText != null)
            slotTitleText.text = data.saveName;
        if (playtimeText != null)
            playtimeText.text = $"Playtime: {SaveManager.Instance.FormatPlaytime(data.playtimeSeconds)}";
        if (dateText != null)
            dateText.text = data.dateTime;

        if (data.activePartyNames != null && memberPortraitParent != null && memberPortraitPrefab != null)
        {
            foreach (var memberName in data.activePartyNames)
            {
                var so = SaveManager.Instance.FindCharacterSO(memberName);
                if (so == null)
                {
                    Debug.LogWarning($"[SAVE SLOT UI] Could not find CharacterStatsSO for {memberName}");
                    continue;
                }

                GameObject portrait = Instantiate(memberPortraitPrefab, memberPortraitParent);
                var img = portrait.GetComponent<Image>();
                if (img != null)
                {
                    if (so.headPortrait != null) img.sprite = so.headPortrait;
                    else if (so.portrait != null) img.sprite = so.portrait;
                }
            }
        }
    }
}