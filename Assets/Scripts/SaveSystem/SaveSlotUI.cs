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
            Debug.Log($"[SAVE SLOT UI] Building {data.activePartyNames.Count} portraits. " +
                $"Parent size: {((RectTransform)memberPortraitParent).rect.width}x{((RectTransform)memberPortraitParent).rect.height}");

            foreach (var memberName in data.activePartyNames)
            {
                var so = SaveManager.Instance.FindCharacterSO(memberName);
                if (so == null)
                {
                    Debug.LogWarning($"[SAVE SLOT UI] Could not find CharacterStatsSO for {memberName}");
                    continue;
                }

                Debug.Log($"[SAVE SLOT UI] Found SO for {memberName}. " +
                    $"headPortrait={so.headPortrait != null} portrait={so.portrait != null}");

                GameObject portrait = Instantiate(memberPortraitPrefab, memberPortraitParent);
                portrait.SetActive(true);

                var rt = portrait.GetComponent<RectTransform>();
                if (rt != null)
                    Debug.Log($"[SAVE SLOT UI] Portrait instantiated. Size: {rt.rect.width}x{rt.rect.height} " +
                        $"localScale: {rt.localScale} anchoredPos: {rt.anchoredPosition}");

                var img = portrait.GetComponent<Image>();
                if (img == null)
                    img = portrait.GetComponentInChildren<Image>();

                if (img != null)
                {
                    Debug.Log($"[SAVE SLOT UI] Image found. Current sprite={img.sprite} color={img.color}");

                    if (so.headPortrait != null) img.sprite = so.headPortrait;
                    else if (so.portrait != null) img.sprite = so.portrait;

                    // Force full visibility regardless of prefab defaults
                    var c = img.color;
                    c.a = 1f;
                    img.color = c;
                    img.enabled = true;

                    Debug.Log($"[SAVE SLOT UI] After assign - sprite={img.sprite} color={img.color} enabled={img.enabled}");
                }
                else
                {
                    Debug.LogError("[SAVE SLOT UI] memberPortraitPrefab has NO Image component anywhere!");
                }
            }
        }
    }
        }