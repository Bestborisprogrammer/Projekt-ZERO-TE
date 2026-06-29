using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitleText;
    public Button retryButton;
    public Button loadLastSaveButton;
    public Button mainMenuButton;

    [Header("Scene")]
    public string mainMenuScene = "MainMenu";
    public string overworldScene = "overworldScene";

    private static List<CombatSnapshot> partySnapshot = new();
    private static List<InventoryItemSave> inventorySnapshot = new();
    private static int goldSnapshot = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Start()
    {
        retryButton?.onClick.AddListener(OnRetry);
        loadLastSaveButton?.onClick.AddListener(OnLoadLastSave);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    public static void SnapshotBeforeBattle()
    {
        partySnapshot.Clear();
        inventorySnapshot.Clear();

        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.activeParty)
            {
                partySnapshot.Add(new CombatSnapshot
                {
                    characterName = member.Name,
                    hp = member.currentHP,
                    mana = member.currentMana
                });
            }
        }

        if (InventoryManager.Instance != null)
        {
            foreach (var item in InventoryManager.Instance.items)
            {
                inventorySnapshot.Add(new InventoryItemSave
                {
                    itemSOName = item.itemData.itemName,
                    quantity = item.quantity
                });
            }
        }

        goldSnapshot = GoldManager.Instance != null ? GoldManager.Instance.gold : 0;

        Debug.Log($"[GAME OVER] Snapshot: {partySnapshot.Count} members, " +
            $"{inventorySnapshot.Count} item stacks, Gold:{goldSnapshot}");
    }

    public void ShowGameOver()
    {
        Debug.Log("[GAME OVER] Showing screen");
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
        gameOverTitleText.text = "Your party has fallen...";

        bool hasAnySave = SaveManager.Instance.SlotExists(SaveManager.AutoSaveSlot);
        for (int i = 0; i < SaveManager.MaxSlots && !hasAnySave; i++)
            if (SaveManager.Instance.SlotExists(i)) hasAnySave = true;

        loadLastSaveButton.interactable = hasAnySave;
        Time.timeScale = 0f;
    }

    void OnRetry()
    {
        Debug.Log("[GAME OVER] Retry pressed");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);

        // Restore HP to 50% of pre-battle value, full mana restore
        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.activeParty)
            {
                var snap = partySnapshot.Find(s => s.characterName == member.Name);
                if (snap != null)
                {
                    member.currentHP = Mathf.Max(1, Mathf.RoundToInt(snap.hp * 0.5f));
                    member.currentMana = snap.mana;
                }
                else
                {
                    member.currentHP = Mathf.Max(1, member.MaxHP / 2);
                }
            }
        }

        // Restore inventory to pre-battle state
        if (InventoryManager.Instance != null && inventorySnapshot.Count > 0)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var itemSave in inventorySnapshot)
            {
                var so = SaveManager.Instance.FindItemSO(itemSave.itemSOName);
                if (so == null) continue;
                InventoryManager.Instance.items.Add(new InventoryItem(so, itemSave.quantity));
            }
        }

        // Reset the encounter trigger so it fires again on retry
        ResetEncounterForRetry();

        EncounterManager.CurrentEnemies.Clear();
        SceneManager.LoadScene(overworldScene);
    }

    void ResetEncounterForRetry()
    {
        if (EncounterManager.LastEncounterWasScripted)
        {
            // Clear the DialogueTrigger's one-time flag so the
            // cutscene/dialogue runs again when player walks back in
            string key = EncounterManager.LastEncounterTriggerID;
            if (!string.IsNullOrEmpty(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"[GAME OVER] Cleared scripted encounter flag: {key}");
            }
            else
            {
                Debug.LogWarning("[GAME OVER] Scripted encounter had no DialogueTrigger key set - " +
                    "assign 'Dialogue Trigger Save Key' on CutsceneManager in the Inspector");
            }
        }
        else if (!string.IsNullOrEmpty(EncounterManager.LastEncounterTriggerID))
        {
            // Normal encounter - clear its uniqueID flag so it re-spawns
            PlayerPrefs.DeleteKey(EncounterManager.LastEncounterTriggerID);
            PlayerPrefs.Save();
            Debug.Log($"[GAME OVER] Cleared normal encounter flag: {EncounterManager.LastEncounterTriggerID}");
        }
    }

    void OnLoadLastSave()
    {
        Debug.Log("[GAME OVER] Loading most recent save");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);

        int mostRecentSlot = FindMostRecentSlot();
        if (mostRecentSlot != int.MinValue)
            SaveManager.Instance.LoadFromSlot(mostRecentSlot);
        else
            Debug.LogWarning("[GAME OVER] No save found!");
    }

    int FindMostRecentSlot()
    {
        int bestSlot = int.MinValue;
        System.DateTime bestTime = System.DateTime.MinValue;

        for (int i = SaveManager.AutoSaveSlot; i < SaveManager.MaxSlots; i++)
        {
            var preview = SaveManager.Instance.LoadSlotPreview(i);
            if (preview == null || preview.isEmpty) continue;
            if (System.DateTime.TryParse(preview.dateTime, out var parsed))
            {
                if (parsed > bestTime)
                {
                    bestTime = parsed;
                    bestSlot = i;
                }
            }
        }

        Debug.Log($"[GAME OVER] Most recent slot: {bestSlot}");
        return bestSlot;
    }

    void OnMainMenu()
    {
        Debug.Log("[GAME OVER] Main menu");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);
        SceneManager.LoadScene(mainMenuScene);
    }
}

[System.Serializable]
public class CombatSnapshot
{
    public string characterName;
    public int hp;
    public int mana;
}