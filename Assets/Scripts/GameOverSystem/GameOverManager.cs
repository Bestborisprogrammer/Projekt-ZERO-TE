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

    // Snapshot of state when battle started - for retry restore
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

    // Call this RIGHT BEFORE starting any encounter
    // so we have a clean snapshot to restore on retry
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

        Debug.Log($"[GAME OVER] Snapshot taken. " +
            $"Party: {partySnapshot.Count} members, " +
            $"Items: {inventorySnapshot.Count} stacks, " +
            $"Gold: {goldSnapshot}");
    }

    public void ShowGameOver()
    {
        Debug.Log("[GAME OVER] Showing game over screen");
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
        gameOverTitleText.text = "Your party has fallen...";

        bool hasAnySave = false;
        if (SaveManager.Instance.SlotExists(SaveManager.AutoSaveSlot))
            hasAnySave = true;
        for (int i = 0; i < SaveManager.MaxSlots && !hasAnySave; i++)
            if (SaveManager.Instance.SlotExists(i)) hasAnySave = true;

        loadLastSaveButton.interactable = hasAnySave;
        Time.timeScale = 0f;
    }

    void OnRetry()
    {
        Debug.Log("[GAME OVER] Retry – restoring pre-battle state");
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);

        // Restore HP/mana to pre-battle values (at 50% hp cap)
        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.activeParty)
            {
                var snap = partySnapshot.Find(s => s.characterName == member.Name);
                if (snap != null)
                {
                    member.currentHP = Mathf.Max(1, Mathf.RoundToInt(snap.hp * 0.5f));
                    member.currentMana = snap.mana; // full mana restored
                }
                else
                {
                    member.currentHP = Mathf.Max(1, member.MaxHP / 2);
                }
            }
        }

        // Restore inventory to pre-battle snapshot
        if (InventoryManager.Instance != null && inventorySnapshot.Count > 0)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var itemSave in inventorySnapshot)
            {
                var so = SaveManager.Instance.FindItemSO(itemSave.itemSOName);
                if (so == null) continue;
                InventoryManager.Instance.items.Add(new InventoryItem(so, itemSave.quantity));
            }
            Debug.Log($"[GAME OVER] Inventory restored to {inventorySnapshot.Count} stacks");
        }

        EncounterManager.CurrentEnemies.Clear();
        SceneManager.LoadScene(overworldScene);
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
            Debug.LogWarning("[GAME OVER] No save found to load!");
    }

    int FindMostRecentSlot()
    {
        int bestSlot = int.MinValue;
        System.DateTime bestTime = System.DateTime.MinValue;

        // Check autosave + all 10 slots
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

        Debug.Log($"[GAME OVER] Most recent slot found: {bestSlot}");
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